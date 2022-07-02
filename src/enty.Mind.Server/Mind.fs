namespace enty.Mind.Server

open enty.Core
open enty.Mind

[<RequireQualifiedAccess>]
type WishOrderingKey =
    | ByCreation
    | ByUpdated
    | ById

type WishOrdering =
    { Descending: bool
      Key: WishOrderingKey }

type IMindService =
    abstract Remember: eid: EntityId * sense: Sense -> Async<unit>
    abstract Forget: eid: EntityId -> Async<unit>
    abstract Wish: wish: Wish * WishOrdering * offset: int * limit: int -> Async<EntityId[] * int>
    abstract GetEntities: eids: EntityId[] -> Async<Entity[]>
