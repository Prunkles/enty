namespace enty.Mind

open enty.Core
open FSharp.Control


type IMindService =
    abstract Remember: entityId: EntityId * sense: Sense -> Async<unit>
    abstract Forget: entityId: EntityId -> Async<unit>
    
    abstract GetSense: entityId: EntityId -> Async<Sense>
    
    abstract Wish: wish: Wish -> AsyncSeq<EntityId>
