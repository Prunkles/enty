namespace enty.Mind.Server

open System
open System.Threading.Tasks
open FSharp.Control
open Grpc.Core
open Google.Protobuf.WellKnownTypes

open enty.Utils
open enty.Core
open enty.Mind

open SenseJToken


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
                | c -> invalidOp $"Invalid proto path entry: %A{c}"
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
        | Proto.Wish.WishOneofCase.Any ->
            let pAny = protoWish.Any
            let path = protoPathToPath pAny.Path
            Wish.Any path
        | Proto.Wish.WishOneofCase.Operator ->
            let pOperator = protoWish.Operator
            let wishOperator =
                match pOperator.OperatorCase with
                | Proto.WishOperator.OperatorOneofCase.And ->
                    let lhs = ofProto pOperator.And.Lhs
                    let rhs = ofProto pOperator.And.Rhs
                    WishOperator.And (lhs, rhs)
                | Proto.WishOperator.OperatorOneofCase.Or ->
                    let lhs = ofProto pOperator.Or.Lhs
                    let rhs = ofProto pOperator.Or.Rhs
                    WishOperator.Or (lhs, rhs)
                | Proto.WishOperator.OperatorOneofCase.Not ->
                    let innerWish = ofProto pOperator.Not.Wish
                    WishOperator.Not innerWish
                | c -> invalidOp $"Invalid proto wish: %A{c}"
            Wish.Operator wishOperator
        | c -> invalidOp $"Invalid proto wish: %A{c}"


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
            let! requests = requestStream.ReadAllAsync() |> AsyncSeq.ofAsyncEnum |> AsyncSeq.toArrayAsync
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
