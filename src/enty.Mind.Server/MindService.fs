namespace enty.Mind.Server

open System
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Google.Protobuf.Collections
open Grpc.Core
open Google.Protobuf.WellKnownTypes
open enty.Utils
open enty.Core
open enty.Mind
//open enty.Mind.Server.Api

//open WishParsing
//open SenseParsing
open SenseJToken

//type JsonValue = Newtonsoft.Json.Linq.JToken
//
//type MindApi(mind: IMind) =
//    interface IMindApi<JsonValue> with
//        member this.Remember(request) = async {
//            let eid = EntityId request.EntityId
//            let senseString = request.SenseString
//            let sense = Sense.parse senseString |> Result.getOk
//            do! mind.Remember(eid, sense)
//        }
//        member this.Forget(request) = async {
//            do! mind.Forget(EntityId request.EntityId)
//        }
//        member this.GetEntities(request) = async {
//            let eids = request.EntityIds |> Array.map EntityId
//            let! entities = mind.GetEntities(eids)
//            let entityDtos =
//                entities
//                |> Array.map (fun entity ->
//                    let (EntityId eidG) = entity.Id
//                    let senseDto = SenseDto (Sense.toJToken entity.Sense)
//                    { Id = eidG; Sense = senseDto }
//                )
//            return { Entities= entityDtos }
//        }
//        member this.Wish(request) = async {
//            let wish = Wish.parse request.WishString |> Result.getOk
//            let! eids, total = mind.Wish(wish, request.Offset, request.Limit)
//            let eidGs = eids |> Array.map (fun (EntityId x) -> x)
//            let response =
//                { EntityIds = eidGs; Total = total }
//            return response
//        }

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

    let private protoPathToPath (protoPath: Proto.WishPath) : WishPathEntry list =
        [
            for pEntry in protoPath.Entries do
                match pEntry.EntryCase with
                | Proto.WishPathEntry.EntryOneofCase.List ->
                    WishPathEntry.ListEntry
                | Proto.WishPathEntry.EntryOneofCase.MapKey ->
                    WishPathEntry.MapEntry pEntry.MapKey
                | _ -> failwith "unreachable"
        ]

    let rec ofProto (protoWish: Proto.Wish) : Wish =
        match protoWish.WishCase with
        | Proto.Wish.WishOneofCase.ValueIs ->
            let valueIs = protoWish.ValueIs
            let path = valueIs.Path |> protoPathToPath
            Wish.ValueIs (path, valueIs.Value)
        | Proto.Wish.WishOneofCase.ListContains ->
            let path = protoPathToPath protoWish.ListContains.Path
            Wish.ListContains (path, protoWish.ListContains.Value)
        | Proto.Wish.WishOneofCase.MapFieldIs ->
            let pMapFieldIs = protoWish.MapFieldIs
            let path = protoPathToPath pMapFieldIs.Path
            Wish.MapFieldIs (path, pMapFieldIs.Key, pMapFieldIs.Value)
        | _ -> failwith "unreachable"


type GrpcServerMindService(mind: IMind) =
    inherit enty.Mind.Proto.MindService.MindServiceBase()

    override this.Forget(request, context) = task {
        let eid = request.Eid |> Guid.Parse |> EntityId
        do! mind.Forget(eid)
        return Empty()
    }
    override this.Remember(request, context) = task {
        let eid = EntityId (Guid.Parse(request.Eid))
        let sense = Sense.ofProto request.Sense
        do! mind.Remember(eid, sense)
        let response = Empty()
        return response
    }
    override this.GetEntities(requestStream, responseStream, context) =
        task {
            // TODO: Process stream-like
            let! requests = task {
                let requests = ResizeArray()
                let! moved = requestStream.MoveNext(context.CancellationToken)
                let mutable moved = moved
                while moved do
                    requests.Add(requestStream.Current)
                    let! moved' = requestStream.MoveNext(context.CancellationToken)
                    moved <- moved'
                return requests
            }
            let eids = requests |> Seq.map (fun rq -> EntityId (Guid.Parse(rq.Eid))) |> Seq.toArray
            let! entities = mind.GetEntities(eids)
            for entity in entities do
                let response =
                    Proto.GetEntitiesResponse(
                        Eid = (entity.Id |> EntityId.Unwrap |> string),
                        Sense = (entity.Sense |> Sense.toProto)
                    )
                do! responseStream.WriteAsync(response)
        } :> Task
    override this.Wish(request, context) = task {
        let wish = Wish.ofProto request.Wish
        let! eids, total = mind.Wish(wish, request.Offset, request.Limit)
        let eidSs = eids |> Array.map (fun (EntityId x ) -> string x)
        let response = Proto.WishResponse(Total = total)
        response.Eids.AddRange(eidSs)
        return response
    }
