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

open WishParsing
open SenseParsing
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

type GrpcMindService(mind: IMind) =
    inherit enty.Mind.Proto.MindService.MindServiceBase()
    
    override this.Forget(request, context) = task {
        let eid = request.Eid |> Guid.Parse |> EntityId
        do! mind.Forget(eid)
        return Empty()
    }
    override this.Remember(request, context) = task {
        let eid = EntityId (Guid.Parse(request.Eid))
        let sense = request.SenseString |> Sense.parse |> Result.getOk
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
                let response = Proto.GetEntitiesResponse()
                do! responseStream.WriteAsync(response)
        } :> Task
    override this.Wish(request, context) = task {
        let wish = Wish.parse request.WishString |> Result.getOk
        let! eids, total = mind.Wish(wish, request.Offset, request.Limit)
        let eidSs = eids |> Array.map (fun (EntityId x ) -> string x)
        let response = Proto.WishResponse(Total = total)
        response.Eids.AddRange(eidSs)
        return response
    }