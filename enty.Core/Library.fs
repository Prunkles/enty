namespace enty.Core

open System

type EntityId = EntityId of Guid

type Sense = Sense

module Sense =
    let tryGet (key: string) (sense: Sense) : string option =
        failwith "unimpl"
