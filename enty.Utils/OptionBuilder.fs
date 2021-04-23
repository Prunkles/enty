[<AutoOpen>]
module enty_Utils_OptionBuilder

type OptionBuilder() =
    member _.Bind(x, f) = Option.bind f x
    member _.Return(x) = Some x
    member _.ReturnFrom(x: _ option) = x
[<AutoOpen>]
module OptionBuilderImpl =
    let option = OptionBuilder()
