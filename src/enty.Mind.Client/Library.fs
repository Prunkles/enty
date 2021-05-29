namespace enty.Mind.Client

open enty.Core
open enty.Mind


type IMindService =
    abstract Wish: wish: Wish * offset: int * limit: int -> Async<EntityId[] * int>
    abstract Remember: eid: EntityId * sense: Sense -> Async<unit>
    abstract Forget: eid: EntityId -> Async<unit>
    abstract GetEntities: eids: EntityId[] -> Async<Entity[]>


module GrpcMindServiceImpl =

    open System
    open System.Threading
    
    module Sense =
        
        type ProtoSense = enty.Mind.Proto.Sense
        
        let rec toProto (sense: Sense) : ProtoSense =
            let protoSense = ProtoSense()
            match sense with
            | Sense.Value value ->
                let protoSenseValue = Proto.SenseValue(Value = value)
                protoSense.SenseValue <- protoSenseValue
            | Sense.List ls ->
                let protoSenseList = Proto.SenseList()
                protoSenseList.Elements.AddRange(ls |> Seq.map toProto)
                protoSense.SenseList <- protoSenseList
            | Sense.Map mp ->
                let protoSenseMap = Proto.SenseMap()
                for KeyValue(k, v) in mp do
                    protoSenseMap.Map.Add(k, toProto v)
                protoSense.SenseMap <- protoSenseMap
            protoSense
        
        let rec ofProto (protoSense: ProtoSense) : Sense =
            match protoSense.SenseCase with
            | ProtoSense.SenseOneofCase.SenseValue ->
                let value = protoSense.SenseValue.Value
                Sense.Value value
            | ProtoSense.SenseOneofCase.SenseList ->
                let ls = protoSense.SenseList.Elements
                let ls = ls |> Seq.map ofProto |> Seq.toList
                Sense.List ls
            | ProtoSense.SenseOneofCase.SenseMap ->
                let mp = protoSense.SenseMap.Map
                let mp = mp |> Seq.map (fun (KeyValue(k, v)) -> k, ofProto v) |> Map.ofSeq
                Sense.Map mp
            | _ -> invalidArg (nameof protoSense) ""
    
    module Wish =

        open Google.Protobuf.WellKnownTypes
        
        let private pathToProtoPath (path: WishPathEntry list) : Proto.WishPath =
            let pWishPath = Proto.WishPath()
            for entry in path do
                match entry with
                | WishPathEntry.ListEntry ->
                    let pEntry = Proto.WishPathEntry()
                    pEntry.List <- Empty()
                    pWishPath.Entries.Add(pEntry)
                | WishPathEntry.MapEntry key ->
                    let pEntry = Proto.WishPathEntry()
                    pEntry.MapKey <- key
                    pWishPath.Entries.Add(pEntry)
            pWishPath
        
        let rec toProto (wish: Wish) : Proto.Wish =
            let pWish = Proto.Wish()
            match wish with
            | Wish.ValueIs (path, value) ->
                let pValueIs = Proto.WishValueIs()
                pValueIs.Path <- pathToProtoPath path
                pValueIs.Value <- value
                pWish.ValueIs <- pValueIs
            | Wish.ListContains (path, value) ->
                let pListContains = Proto.WishListContains()
                pListContains.Path <- pathToProtoPath path
                pListContains.Value <- value
                pWish.ListContains <- pListContains
            | Wish.MapFieldIs (path, key, value) ->
                let pMapFieldIs = Proto.WishMapFieldIs(Key=key, Value=value)
                pMapFieldIs.Path <- pathToProtoPath path
                pWish.MapFieldIs <- pMapFieldIs
            | Wish.Operator op ->
                let pOperator = Proto.WishOperator()
                match op with
                | WishOperator.And (lhs, rhs) ->
                    pOperator.And <- Proto.WishAndOperator(Lhs = toProto lhs, Rhs = toProto rhs)
                | WishOperator.Or (lhs, rhs) ->
                    pOperator.Or <- Proto.WishOrOperator(Lhs = toProto lhs, Rhs = toProto rhs)
                | WishOperator.Not (op) ->
                    pOperator.Not <- Proto.WishNotOperator(Wish = toProto op)
                pWish.Operator <- pOperator
            pWish
    
     
    open enty.Mind.Proto
    open FSharp.Control.Tasks.V2
    
    type GrpcClientMindService(client: MindService.MindServiceClient) =
        interface IMindService with
            member this.Forget(eid) = async {
                let eidS = let (EntityId eidG) = eid in string eidG
                let request = ForgetRequest(Eid = eidS)
                let! response = client.ForgetAsync(request).ResponseAsync |> Async.AwaitTask
                return ()
            }
            member this.GetEntities(eids) = async {
                return! Async.AwaitTask(task {
                    let call = client.GetEntities()
                    for EntityId eidG in eids do
                        let request = GetEntitiesRequest(Eid = string eidG)
                        do! call.RequestStream.WriteAsync(request)
                    let responseStream = call.ResponseStream
                    let! responses = task {
                        let requests = ResizeArray()
                        let! moved = responseStream.MoveNext(CancellationToken.None)
                        let mutable moved = moved
                        while moved do
                            requests.Add(responseStream.Current)
                            let! moved' = responseStream.MoveNext(CancellationToken.None)
                            moved <- moved'
                        return requests
                    }
                    let entities =
                        responses
                        |> Seq.map (fun response ->
                            { Id = EntityId (Guid.Parse(response.Eid))
                              Sense = Sense.ofProto response.Sense }
                        )
                        |> Seq.toArray
                    return entities
                })
            }
            member this.Remember(eid, sense) = async {
                let request = RememberRequest()
                request.Eid <- eid |> EntityId.Unwrap |> string
                request.Sense <- sense |> Sense.toProto
                let! response = client.RememberAsync(request).ResponseAsync |> Async.AwaitTask
                return ()
            }
            member this.Wish(wish, offset, limit) = async {
                let request = WishRequest()
                request.Wish <- Wish.toProto wish
                request.Offset <- offset
                request.Limit <- limit
                let! response = client.WishAsync(request).ResponseAsync |> Async.AwaitTask
                let eids = response.Eids |> Seq.map (Guid.Parse >> EntityId) |> Seq.toArray
                return eids, response.Total
            }

//open System.Net.Http
//open System.Net.Http.Headers
//open System.Net.Mime
//open FSharp.Control.Tasks.V2
//open Khonsu.Coding.Json
//open Khonsu.Coding.Json.Net
//open enty.Core
//open enty.Mind
//open enty.Mind.Server.Api
//
//[<RequireQualifiedAccess>]
//module Sense =
//
//    open Newtonsoft.Json.Linq
//    
//    let rec ofJToken (token: JToken) : Sense =
//        match token.Type with
//        | JTokenType.Array ->
//            let jArr = token.Value<JArray>()
//            let ls = jArr |> Seq.map ofJToken |> Seq.toList
//            Sense.List ls
//        | JTokenType.String ->
//            let jVal = token.Value<JValue>()
//            let value = jVal.ToString()
//            Sense.Value value
//        | JTokenType.Object ->
//            let jObj = token.Value<JObject>()
//            let mp = jObj.Properties() |> Seq.map (fun jp -> jp.Name, ofJToken jp.Value) |> Map.ofSeq
//            Sense.Map mp
//        | _ -> invalidOp ""
//    
//    let rec toJToken (sense: Sense) : JToken =
//        match sense with
//        | Sense.Value value ->
//            upcast JValue(value)
//        | Sense.List ls ->
//            let content = ls |> Seq.map toJToken |> Seq.toArray
//            upcast JArray(content)
//        | Sense.Map mp ->
//            let content = mp |> Seq.map (fun (KeyValue (k, v)) -> JProperty(k, toJToken v))
//            upcast JObject(content)
//    
//
//module MindApi =
//    
//    open System
//    open System.Net.Http
//    open System.Net.Http.Headers
//    open System.Net.Mime
//    open System.Threading.Tasks
//    open FSharp.Control.Tasks.V2
//    open Khonsu.Coding.Json
//    open Khonsu.Coding.Json.Net
//    
//    let remember (client: HttpClient) (jsonEncoding: IJsonEncoding<JsonValue>) (eidGuid: Guid) (senseString: string) : Task<unit> = task {
//        let path = "/remember"
//        let json = Encode.object [
//            "eid", Encode.guid eidGuid
//            "sense", sense
//        ]
//        
//        use content = new StringContent(json.ToString())
//        content.Headers.ContentType <- MediaTypeHeaderValue.Parse("application/json")
//        let! response = client.PostAsync(path, content)
//        return ()
//    }
//    
//    let forget (client: HttpClient) (eidGuid: Guid) : Task<unit> = task {
//        let path = "/forget"
//        let json = Encode.object [
//            "eid", Encode.guid eidGuid
//        ]
//        use content = new StringContent(json.ToString())
//        content.Headers.ContentType <- MediaTypeHeaderValue.Parse("application/json")
//        let! response = client.PostAsync(path, content)
//        return ()
//    }
//    
//    let wish (client: HttpClient) (wishString: string) (offset: int) (limit: int) : Task<Guid[] * int> = task {
//        let path = "/wish"
//        let json = Encode.object [
//            "wishString", Encode.string wishString
//            "pgn", Encode.object [
//                "offset", Encode.int offset
//                "limit", Encode.int limit
//            ]
//        ]
//        use content = new StringContent(json.ToString())
//        content.Headers.ContentType <- MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json)
//        let! response = client.PostAsync(path, content)
//        
//        let! json = response.Content.ReadAsStringAsync()
//        let decoder = 
//            Decode.object (fun get ->
//                let eidGs = get.Required.Field "eids" (Decode.array Decode.guid)
//                let total = get.Required.Field "pgn" (Decode.field "total" Decode.int)
//                eidGs, total
//            )
//        let result = Decode.fromString decoder json
//        return result |> function Ok x -> x | _ -> failwith ""
//    }
//    
//    let getEntities (client: HttpClient) (eidGs: Guid[]) : Task<(Guid * JsonValue)[]> = task {
//        let path = "/getEntities"
//        let requestJson = Encode.object [
//            "eids", Encode.array (eidGs |> Array.map Encode.guid)
//        ]
//        use content = new StringContent(requestJson.ToString())
//        content.Headers.ContentType <- MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json)
//        
//        printfn $"BA: {client.BaseAddress}; path: {path}"
//        let! response = client.PostAsync(path, content)
//        
//        let! responseString = response.Content.ReadAsStringAsync()
//        let decoder =
//            Decode.field "entities" (Decode.array (Decode.object (fun get ->
//                let eid = get.Required.Field "id" Decode.guid
//                let sense = get.Required.Field "sense" (fun _ -> Ok)
//                eid, sense
//            )))
//        let result = Decode.fromString decoder responseString
//        return result |> function Ok x -> x | Error err -> failwith $"{err}"
//    }
//
//
//type ClientMindApi(client: HttpClient) =
//    let jsonEncoding = ThothJsonEncoding() :> IJsonEncoding<_>
//    let jsonDecoding = ThothJsonDecoding() :> IJsonDecoding<_>
//    
//    let postR (route: string) (request: 'q) encoder = task {
//        let requestString = jsonEncoding.EncodeToString(encoder request jsonEncoding)
//        use content = new StringContent(requestString)
//        content.Headers.ContentType <- MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json)
//        
//        let! httpResponseMsg = client.PostAsync(route, content)
//        return! httpResponseMsg.Content.ReadAsStringAsync()
//    }
//    let mkResponse bodyString decoder : 'p =
//        let responseResult = jsonDecoding.DecodeFromString(bodyString, decoder jsonDecoding)
//        match responseResult with
//        | Ok response -> response
//        | Error err -> failwith $"{err}"
//    
//    interface IMindApi<JsonValue> with
//        member this.Forget(request) = async {
//            let! _ = postR "/forget" request (ForgetRequest.Encoder()) |> Async.AwaitTask
//            return ()
//        }
//        member this.GetEntities(request) = async {
//            let! entities = MindApi.getEntities client request.EntityIds |> Async.AwaitTask
//            let entities = entities |> Array.map (fun (eidG, sense) -> { Id = eidG; Sense = SenseDto sense })
//            let response = { Entities = entities }
//            return response
//        }
//        member this.Remember(request) = async {
//            let (SenseDto sense) = request.Sense
//            do! MindApi.remember client request.EntityId sense |> Async.AwaitTask
//        }
//        member this.Wish(request) = async {
//            let! (eids, total) = MindApi.wish client request.WishString request.Offset request.Limit |> Async.AwaitTask
//            let response = { EntityIds = eids; Total = total }
//            return response
//        }
