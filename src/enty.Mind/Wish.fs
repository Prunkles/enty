namespace enty.Mind

[<RequireQualifiedAccess>]
type WishPathEntry =
    | MapEntry of key: string
    | ListEntry

[<RequireQualifiedAccess>]
type WishAst =
    | MapFieldIs of path: WishPathEntry list * key: string * value: string
    | ListContains of path: WishPathEntry list * value: string
    | ValueIs of path: WishPathEntry list * value: string
    | And of WishAst * WishAst
    | Or of WishAst * WishAst
    | Not of WishAst

type Wish = WishAst
