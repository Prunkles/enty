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
            | Wish.Any path ->
                let pAny = Proto.WishAny()
                pAny.Path <- pathToProtoPath path
                pWish.Any <- pAny
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
                    let requestStream = call.RequestStream
                    let responseStream = call.ResponseStream
                    for EntityId eidG in eids do
                        let request = GetEntitiesRequest(Eid = string eidG)
                        do! requestStream.WriteAsync(request)
                    do! requestStream.CompleteAsync()
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
