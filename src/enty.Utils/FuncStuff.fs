[<AutoOpen>]
module FuncStuff

let inline ( ^ ) f x = f x

let inline ( !> ) (x: ^a) : ^b = ((^a or ^b) : (static member op_Implicit: ^a -> ^b) x)

[<AutoOpen>]
module Todo =
    [<System.Obsolete("TODO", false)>]
    let inline todo<'a> : 'a =
        raise (System.NotImplementedException("TODO"))

let (|Const|) x _ = x

[<return: Struct>]
let inline (|Equals|_|) x y = if x = y then ValueSome Equals else ValueNone
