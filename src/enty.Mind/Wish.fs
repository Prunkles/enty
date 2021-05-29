//[<System.Obsolete>]
namespace enty.Mind


[<RequireQualifiedAccess>]
type WishPathEntry =
    | MapEntry of key: string
    | ListEntry

type [<RequireQualifiedAccess>]
    Wish =
    | MapFieldIs of path: WishPathEntry list * key: string * value: string
    | ListContains of path: WishPathEntry list * value: string
    | ValueIs of path: WishPathEntry list * value: string
    | Operator of WishOperator

and [<RequireQualifiedAccess>]
    WishOperator =
    | And of Wish * Wish
    | Or of Wish * Wish
    | Not of Wish


