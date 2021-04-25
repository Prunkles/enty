namespace enty.Core


type Symbol = Symbol of string

type Association =
    { From: Symbol
      Into: Symbol }

type Sense =
    { References: Sense list
      Associations: Association list }

[<RequireQualifiedAccess>]
module Sense =
    let tryGet (key: string) (sense: Sense) : string option =
        failwith "unimpl"
