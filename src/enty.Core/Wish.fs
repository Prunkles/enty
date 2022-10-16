namespace enty.Core


[<RequireQualifiedAccess>]
type WishPathEntry =
    | MapEntry of key: string
    | ListEntry

type [<RequireQualifiedAccess>]
    Wish =
    | MapFieldIs of path: WishPathEntry list * key: string * value: string
    | ListContains of path: WishPathEntry list * value: string
    | AtomIs of path: WishPathEntry list * value: string
    | Any of path: WishPathEntry list
    | Operator of WishOperator

and [<RequireQualifiedAccess>]
    WishOperator =
    | And of Wish * Wish
    | Or of Wish * Wish
    | Not of Wish
