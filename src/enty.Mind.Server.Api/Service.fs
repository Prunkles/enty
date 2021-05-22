namespace global

//namespace enty.Mind.Server.Api
//
//open enty.Core
//
//
//type GetEntitiesError =
//    | InvalidWishString of errorMessage: string
//    
//
//type IMindService =
//    abstract Remember: eid: EntityId * senseString: string -> Async<unit>
//    abstract Forget: eid: EntityId -> Async<unit>
//    abstract Wish: wishString: string * offset: int * limit: int -> Async<EntityId[] * int>
//    abstract GetEntities: eids: EntityId[] -> Async<Entity[]>
//
//
//type ApiMindService<'JsonValue>(mindApi: IMindApi<'JsonValue>, decodeSense) =
//    interface IMindService with
//        member this.Forget(EntityId eidG) = async {
//            let rq = { EntityId = eidG }
//            let! () = mindApi.Forget(rq)
//            return ()
//        }
//        member this.GetEntities(eids) = async {
//            let eidGs = Array.map (fun (EntityId x) -> x) eids
//            let rq = { EntityIds = eidGs }
//            let! rp = mindApi.GetEntities(rq)
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
//            let! () = mindApi.Remember(rq)
//            return ()
//        }
//        member this.Wish(wishString, offset, limit) = async {
//            let rq =
//                { WishString = wishString
//                  Offset = offset; Limit = limit }
//            let! rp = mindApi.Wish(rq)
//            let eids = rp.EntityIds |> Array.map EntityId
//            let total = rp.Total
//            return eids, total
//        }
