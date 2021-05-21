namespace enty.Mind.Client

open System.Net.Http
//open Newtonsoft.Json.Linq
open enty.Core
open enty.Mind
open enty.Mind.Server.Api

type JsonValue = Newtonsoft.Json.Linq.JToken

[<RequireQualifiedAccess>]
module Sense =

    open Newtonsoft.Json.Linq
    
    let rec ofJToken (token: JToken) : Sense =
        match token.Type with
        | JTokenType.Array ->
            let jArr = token.Value<JArray>()
            let ls = jArr |> Seq.map ofJToken |> Seq.toList
            Sense.List ls
        | JTokenType.String ->
            let jVal = token.Value<JValue>()
            let value = jVal.ToString()
            Sense.Value value
        | JTokenType.Object ->
            let jObj = token.Value<JObject>()
            let mp = jObj.Properties() |> Seq.map (fun jp -> jp.Name, ofJToken jp.Value) |> Map.ofSeq
            Sense.Map mp
        | _ -> invalidOp ""
    
    let rec toJToken (sense: Sense) : JToken =
        match sense with
        | Sense.Value value ->
            upcast JValue(value)
        | Sense.List ls ->
            let content = ls |> Seq.map toJToken |> Seq.toArray
            upcast JArray(content)
        | Sense.Map mp ->
            let content = mp |> Seq.map (fun (KeyValue (k, v)) -> JProperty(k, toJToken v))
            upcast JObject(content)
    

module MindApi =
    
    open System
    open System.Net.Http
    open System.Net.Http.Headers
    open System.Net.Mime
    open System.Threading.Tasks
    open FSharp.Control.Tasks.V2
    open Thoth.Json.Net
    
    let remember (client: HttpClient) (eidGuid: Guid) (sense: JsonValue) : Task<unit> = task {
        let path = "/remember"
        let json = Encode.object [
            "eid", Encode.guid eidGuid
            "sense", sense
        ]
        use content = new StringContent(json.ToString())
        content.Headers.ContentType <- MediaTypeHeaderValue.Parse("application/json")
        let! response = client.PostAsync(path, content)
        return ()
    }
    
    let forget (client: HttpClient) (eidGuid: Guid) : Task<unit> = task {
        let path = "/forget"
        let json = Encode.object [
            "eid", Encode.guid eidGuid
        ]
        use content = new StringContent(json.ToString())
        content.Headers.ContentType <- MediaTypeHeaderValue.Parse("application/json")
        let! response = client.PostAsync(path, content)
        return ()
    }
    
    let wish (client: HttpClient) (wishString: string) (offset: int) (limit: int) : Task<Guid[] * int> = task {
        let path = "/wish"
        let json = Encode.object [
            "wishString", Encode.string wishString
            "pgn", Encode.object [
                "offset", Encode.int offset
                "limit", Encode.int limit
            ]
        ]
        use content = new StringContent(json.ToString())
        content.Headers.ContentType <- MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json)
        let! response = client.PostAsync(path, content)
        
        let! json = response.Content.ReadAsStringAsync()
        let decoder = 
            Decode.object (fun get ->
                let eidGs = get.Required.Field "eids" (Decode.array Decode.guid)
                let total = get.Required.Field "pgn" (Decode.field "total" Decode.int)
                eidGs, total
            )
        let result = Decode.fromString decoder json
        return result |> function Ok x -> x | _ -> failwith ""
    }
    
    let getEntities (client: HttpClient) (eidGs: Guid[]) : Task<(Guid * JsonValue)[]> = task {
        let path = "/getEntities"
        let requestJson = Encode.object [
            "eids", Encode.array (eidGs |> Array.map Encode.guid)
        ]
        use content = new StringContent(requestJson.ToString())
        content.Headers.ContentType <- MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json)
        
        printfn $"BA: {client.BaseAddress}; path: {path}"
        let! response = client.PostAsync(path, content)
        
        let! responseString = response.Content.ReadAsStringAsync()
        let decoder =
            Decode.field "entities" (Decode.array (Decode.object (fun get ->
                let eid = get.Required.Field "id" Decode.guid
                let sense = get.Required.Field "sense" (fun _ -> Ok)
                eid, sense
            )))
        let result = Decode.fromString decoder responseString
        return result |> function Ok x -> x | Error err -> failwith $"{err}"
    }


type ClientMindApi(client) =
    interface IMindApi<JsonValue> with
        member this.Forget(request) = async {
            do! MindApi.forget client request.EntityId |> Async.AwaitTask
        }
        member this.GetEntities(request) = async {
            let! entities = MindApi.getEntities client request.EntityIds |> Async.AwaitTask
            let entities = entities |> Array.map (fun (eidG, sense) -> { Id = eidG; Sense = SenseDto sense })
            let response = { Entities = entities }
            return response
        }
        member this.Remember(request) = async {
            let (SenseDto sense) = request.Sense
            do! MindApi.remember client request.EntityId sense |> Async.AwaitTask
        }
        member this.Wish(request) = async {
            let! (eids, total) = MindApi.wish client request.WishString request.Offset request.Limit |> Async.AwaitTask
            let response = { EntityIds = eids; Total = total }
            return response
        }
