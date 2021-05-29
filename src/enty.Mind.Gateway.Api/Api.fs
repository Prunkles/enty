namespace enty.Mind.Gateway.Api

//open enty.Core
//
//type IMindRawApi<'JsonValue> =
//    abstract Remember: request: RememberRequest -> Async<unit>
//    abstract Forget: request: ForgetRequest -> Async<unit>
//    abstract Wish: request: WishRequest -> Async<WishResponse>
//    abstract GetEntities: request: GetEntitiesRequest -> Async<GetEntitiesResponse<'JsonValue>>
//
//type IMindApi =
//    abstract Remember: eid: EntityId * senseString: string -> Async<unit>
//    abstract Forget: eid: EntityId -> Async<unit>
//    abstract Wish: wishString: string * offset: int * limit: int -> Async<EntityId[] * int>
//    abstract GetEntities: eids: EntityId[] -> Async<Entity[]>
//
//type RawMindApi<'JsonValue>(mindRawApi: IMindRawApi<'JsonValue>, decodeSense) =
//    interface IMindApi with
//        member this.Forget(EntityId eidG) = async {
//            let rq = { EntityId = eidG }
//            let! () = mindRawApi.Forget(rq)
//            return ()
//        }
//        member this.GetEntities(eids) = async {
//            let eidGs = Array.map (fun (EntityId x) -> x) eids
//            let rq = { EntityIds = eidGs }
//            let! rp = mindRawApi.GetEntities(rq)
//            let entities =
//                rp.Entities
//                |> Array.map (fun entityDto ->
//                    { Id = EntityId entityDto.Id
//                      Sense = decodeSense entityDto.Sense }
//                )
//            return entities
//        }
//        member this.Remember(EntityId eidG, senseString) = async {
//            let rq =
//                { EntityId = eidG
//                  SenseString = senseString }
//            let! () = mindRawApi.Remember(rq)
//            return ()
//        }
//        member this.Wish(wishString, offset, limit) = async {
//            let rq =
//                { WishString = wishString
//                  Offset = offset; Limit = limit }
//            let! rp = mindRawApi.Wish(rq)
//            let eids = rp.EntityIds |> Array.map EntityId
//            let total = rp.Total
//            return eids, total
//        }
