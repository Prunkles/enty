[<AutoOpen>]
module enty.Utils.Unreachable

type UnreachableException(message: string) =
    inherit System.Exception(message)
    new() = UnreachableException("This point of code should be unreachable.")
    new(x: obj) = UnreachableException($"This point of code should be unreachable. Received object: %A{x}")

#if !FABLE_COMPILER

open Microsoft.FSharp.Reflection

let unreachable<'a> : 'a =
    if FSharpType.IsFunction(typeof<'a>) then
        FSharpValue.MakeFunction(typeof<'a>, (fun x ->
            raise (UnreachableException(x))
        )) |> unbox
    else
        raise (UnreachableException())

#else

let unreachable<'a> : 'a =
    // TODO: Implement function detection on Fable
    raise (UnreachableException())

#endif

let (|Unreachable|) x = unreachable x
