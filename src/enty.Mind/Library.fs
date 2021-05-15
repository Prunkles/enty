namespace enty.Mind

open enty.Core
open FSharp.Control


//type AsyncPagination<'a> =
//    abstract Page: page: int * pageSize: int -> Async<'a[] * int * int>
//
//type IMindService =
//    abstract Remember: entityId: EntityId * sense: Sense -> Async<unit>
//    abstract Forget: entityId: EntityId -> Async<unit>
//    
//    abstract GetEntities: entityIds: EntityId[] -> Async<Entity[]>
//    
//    abstract Wish: wish: Wish -> AsyncPagination<EntityId>
