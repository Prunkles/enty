namespace enty.Mind

type WishAst =
    | MapFieldIs of path: string list * value: string
    | ListContains of path: string list * value: string
    | ValueIs of path: string list * value: string
    | And of WishAst * WishAst
    | Or of WishAst * WishAst
    | Not of WishAst

type Wish = WishAst
