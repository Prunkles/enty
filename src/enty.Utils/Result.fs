namespace enty.Utils

[<RequireQualifiedAccess>]
module Result =

    let getOk = function
        | Ok x -> x
        | Error e -> invalidOp $"Result is Error %A{e}"
