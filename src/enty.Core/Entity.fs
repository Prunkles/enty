namespace enty.Core

open System

type EntityId = EntityId of Guid with
    static member Unwrap(EntityId eidG) = eidG

type Entity =
    { Id: EntityId
      Sense: Sense }
