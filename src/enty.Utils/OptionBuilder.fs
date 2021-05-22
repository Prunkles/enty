[<AutoOpen>]
module enty_Utils_OptionBuilder

type OptionBuilder() =
    member _.Bind(x, f) = Option.bind f x
    member _.Return(x) = Some x
    member _.ReturnFrom(x: _ option) = x
    member _.Combine(x1, x2) = match x1, x2 with Some x1, Some x2 -> Some (x1, x2) | _ -> None
    member _.Zero() = None

[<AutoOpen>]
module OptionBuilderImpl =
    let option = OptionBuilder()

module Option =
    
    let ofTryByref = function
        | true, v -> Some v
        | false, _ -> None
