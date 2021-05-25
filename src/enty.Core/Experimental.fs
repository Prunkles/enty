[<System.Obsolete "Experimental">]
module enty.Core.Experimental


module Pg =
    
    type Sense =
        | List of Sense list
        | Value of string


[<RequireQualifiedAccess>]
type SenseAtom =
    | Value of string
    | Nil

[<RequireQualifiedAccess>]
type Sense =
    | Pair of Sense * Sense
    | Atom of SenseAtom

type SenseList = Sense list

// ((a . b) . (c . d))

// [[a b] [c d]]
// ((a . (b . Nil)) . ((c (d . Nil)) . Nil))

module Sense =

    let rec (|List|_|) sense =
        match sense with
        | Sense.Atom SenseAtom.Nil -> Some []
        | Sense.Pair (h, List t) -> Some (h :: t)
        | _ -> None
    
    let cons x y = Sense.Pair (x, y)
    let nil = Sense.Atom SenseAtom.Nil

    let (|Cons|Nil|) (senseList: SenseList) =
        match senseList with
        | [] -> Nil
        | t::h -> Cons (t, h)
    
    let atomVal value = Sense.Atom (SenseAtom.Value value)
    
    let rec append (s1: SenseList) (s2: SenseList) : SenseList = 
        match s1 with
        | Nil -> s2
        | Cons (h, t) -> cons h (append t s2)
    
    
    let rec ofList (senses: Sense list) : Sense =
        match senses with
        | h::t -> cons h (ofList t)
        | [] -> nil

    let ofMap (mp: Map<Sense, Sense>) = mp |> Seq.map (fun (KeyValue(k, v)) -> cons k v) |> Seq.toList |> ofList


type SenseBuilder() =
    let listOfSingle x = Sense.cons x Sense.nil
    member _.Yield(value: string): Sense = Sense.atomVal value |> listOfSingle
    member _.Yield(atom: SenseAtom): Sense = Sense.Atom atom |> listOfSingle
    member _.Yield(sense: Sense): Sense = sense |> listOfSingle
    member _.Zero(): Sense = Sense.nil
    member _.Combine(sense1, sense2) = Sense.append sense1 sense2
    member _.Delay(f) = f ()


//[<AutoOpen>]
//module SenseBuilder =
//    let sense = SenseBuilder()
//
//
//sense {
//    "a"
//    "b"
//    "c"
//}

//module Pg =
//    
//    let r: Sense =
//        SenseValue.List [ Sense.atom "" :: [  ] ]

    