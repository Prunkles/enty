namespace enty.Mind

open enty.Core
open FSharp.Control

type Query = Query

type IMindService =
    abstract Add: entityId: EntityId * sense: Sense -> Async<unit>
    abstract Remove: entityId: EntityId -> Async<unit>
    
    abstract GetSense: entityId: EntityId -> Async<Sense>
    
    abstract Query: query: Query -> AsyncSeq<EntityId>
