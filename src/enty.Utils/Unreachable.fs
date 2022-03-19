[<AutoOpen>]
module enty.Utils.Unreachable

type UnreachableException(message: string) =
    inherit System.Exception(message)
    new() = UnreachableException("This point of code should be unreachable.")

let unreachable<'a> : 'a = raise (UnreachableException())

let (|Unreachable|) _ = unreachable
