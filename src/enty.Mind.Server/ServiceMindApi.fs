namespace enty.Mind.Server

open enty.Utils
open enty.Core
open enty.Mind
open enty.Mind.Server.Api

open WishParsing
open SenseJToken

type JsonValue = Newtonsoft.Json.Linq.JToken

type MindApi(mind: IMind) =
    interface IMindApi<JsonValue> with
        member this.Remember(request) = async {
            let eid = EntityId request.EntityId
            let sense = request.Sense |> fun (SenseDto j) -> Sense.ofJToken j
            do! mind.Remember(eid, sense)
        }
        member this.Forget(request) = async {
            do! mind.Forget(EntityId request.EntityId)
        }
        member this.GetEntities(request) = async {
            let eids = request.EntityIds |> Array.map EntityId
            let! entities = mind.GetEntities(eids)
            let entityDtos =
                entities
                |> Array.map (fun entity ->
                    let (EntityId eidG) = entity.Id
                    let senseDto = SenseDto (Sense.toJToken entity.Sense)
                    { Id = eidG; Sense = senseDto }
                )
            return { Entities= entityDtos }
        }
        member this.Wish(request) = async {
            let wish = Wish.parse request.WishString |> Result.getOk
            let! eids, total = mind.Wish(wish, request.Offset, request.Limit)
            let eidGs = eids |> Array.map (fun (EntityId x) -> x)
            let response =
                { EntityIds = eidGs; Total = total }
            return response
        }
