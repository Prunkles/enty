[<System.ObsoleteAttribute>]
module enty.Mind.Obs


//[<RequireQualifiedAccess>]
//type WishOperator<'Term> =
//    | And of 'Term * 'Term
//    | Or of 'Term * 'Term
//    | Not of 'Term
//
//type [<RequireQualifiedAccess>]
//    Wish =
//    | ListContains of Wish
//    | ListItemIs of item: int * Wish
//    | ValueIs of string
//    | Operator of WishOperator<Wish>
//
//
//module Wish =
//    
//    let value v = Wish.ValueIs v
//    
//    let opAnd w1 w2 = Wish.Operator (WishOperator.And (w1, w2))
//    let opOr w1 w2 = Wish.Operator (WishOperator.Or (w1, w2))
//    
//    let ofMap (mp: Map<Wish, Wish>) : Wish =
//        assert (mp.Count > 0)
//        mp
//        |> Seq.map ^fun (KeyValue(k, v)) ->
//            let inner = opAnd (Wish.ListItemIs (0, k)) (Wish.ListItemIs (1, v))
//            Wish.ListContains inner
//        |> Seq.reduceBack opAnd
