namespace enty.Core

open System
open FsToolkit.ErrorHandling
open enty.Utils

// [<RequireQualifiedAccess>]
// type Sense =
//     | Value of string
//     | Map of Map<string, Sense>
//     | List of Sense list

type SenseAtom = SenseAtom of string

type SenseList = SenseList of SenseValue list

and SenseMap = SenseMap of Map<string, SenseValue>

and [<RequireQualifiedAccess>]
    SenseValue =
    | Atom of SenseAtom
    | List of SenseList
    | Map of SenseMap

and Sense = Sense of SenseMap


[<RequireQualifiedAccess>]
module Map =

    let mergeWith (combiner: 'k -> 'v -> 'v -> 'v) (source1: Map<'k, 'v>) (source2: Map<'k, 'v>) : Map<'k ,'v> =
        Map.fold (fun m k v' -> Map.add k (match Map.tryFind k m with Some v -> combiner k v v' | None -> v') m) source1 source2

[<RequireQualifiedAccess>]
module SenseValue =

    let rec tryGet (path: string list) (sense: SenseValue) : SenseValue option =
        match path with
        | key :: tailPath ->
            match sense with
            | SenseValue.Map (SenseMap map) ->
                option {
                    let! innerValue = map |> Map.tryFind key
                    return! tryGet tailPath innerValue
                }
            | SenseValue.List (SenseList ls) ->
                option {
                    let! intKey = Int32.TryParse(key) |> Option.ofTryByref
                    let! innerValue = ls |> List.tryItem intKey
                    return! tryGet tailPath innerValue
                }
            | _ -> None
        | [] -> Some sense

    let tryAsValue (sense: SenseValue) : string option =
        match sense with
        | SenseValue.Atom (SenseAtom v) -> Some v
        | _ -> None

    let tryGetValue (path: string list) (sense: SenseValue) : string option =
        tryGet path sense |> Option.bind tryAsValue

    let tryItem (key: string) (sense: SenseValue) : SenseValue option =
        tryGet [key] sense

    let tryAsList (sense: SenseValue) : SenseValue list option =
        match sense with
        | SenseValue.List (SenseList senses) -> Some senses
        | _ -> None

[<RequireQualifiedAccess>]
module Sense =

    let asValue (sense: Sense) : SenseValue =
        sense |> function Sense senseMap -> SenseValue.Map senseMap

    // let rec tryMerge (sense1: Sense) (sense2: Sense) : Sense option =
    //     match sense1, sense2 with
    //     | Sense.List ls1, Sense.List ls2 ->
    //         Some ^ Sense.List (ls1 @ ls2)
    //     | Sense.Map mp1, Sense.Map mp2 ->
    //         // TODO: Merge fields
    //         Some ^ Sense.Map (Map.toList mp1 @ Map.toList mp2 |> Map.ofList)
    //     | _ -> None

    let empty () : Sense =
        Sense (SenseMap Map.empty)

    let rec merge (sense1: Sense) (sense2: Sense) : Sense =
        // match sense1, sense2 with
        // | Sense.List ls1, Sense.List ls2 ->
        //     Sense.List (ls1 @ ls2)
        // | Sense.Map mp1, Sense.Map mp2 ->
        //     Sense.Map ^ Map.mergeWith merge mp1 mp2
        // | (Sense.Value _ as vl1), (Sense.Value _ as vl2) -> Sense.List [ vl1; vl2 ]
        // | _ -> invalidOp $"Cannot merge {sense1} and {sense2}"
        todo

[<AutoOpen>]
module Builders =

    type SenseListBuilder() =
        member inline _.Source(senses: SenseValue list) = senses
        member inline _.Yield(sense: SenseValue) = [sense]
        member inline _.YieldFrom(senses: SenseValue list) = senses
        member inline _.Zero() = []
        member inline _.Combine(ss1, ss2) = ss1 @ ss2
        member inline _.Delay(f) = f ()
        member inline _.For(sequence: 'a seq, body: 'a -> SenseValue list) = sequence |> Seq.collect body |> Seq.toList
        member inline _.Run(senses: SenseValue list) = SenseList senses

    [<AutoOpen>]
    module SenseListBuilderExtensions =
        type SenseListBuilder with
            member inline this.Yield(senseAtom: SenseAtom) = this.Yield(SenseValue.Atom senseAtom)
            member inline this.Yield(senseMap: SenseMap) = this.Yield(SenseValue.Map senseMap)
            member inline this.Yield(senseList: SenseList) = this.Yield(SenseValue.List senseList)
            member inline this.Yield(atom: string) = this.Yield(SenseAtom atom)
            member inline this.Yield(sense: Sense) = this.Yield(Sense.asValue sense)

            member inline this.Source(values: string list) = values |> List.map (SenseAtom >> SenseValue.Atom)
            member inline this.Source(atoms: SenseAtom list) = atoms |> List.map SenseValue.Atom

    let senseList = SenseListBuilder()

    // ----

    [<AbstractClass>]
    type SenseMapBuilderBase() =
        member inline _.Source(kvs: (string * SenseValue) list) = kvs
        member inline _.Yield((k: string, v: SenseValue)) = [ (k, v) ]
        member inline _.YieldFrom(kvs: (string * SenseValue) list) = kvs
        member inline _.Zero() = []
        member inline _.Combine(ss1, ss2) = ss1 @ ss2
        member inline _.Delay(f) = f ()
        member inline _.For(sequence: 'a seq, body: 'a -> (string * Sense) list) = sequence |> Seq.collect body |> Seq.toList

    [<AutoOpen>]
    module SenseMapBuilderBaseExtensions =
        type SenseMapBuilderBase with
            member inline this.Source(SenseMap senseMap) = senseMap |> Map.toList

            member inline this.Yield((k: string, senseAtom: SenseAtom)) = this.Yield((k, SenseValue.Atom senseAtom))
            member inline this.Yield((k: string, senseList: SenseList)) = this.Yield((k, SenseValue.List senseList))
            member inline this.Yield((k: string, senseMap: SenseMap)) = this.Yield((k, SenseValue.Map senseMap))
            member inline this.Yield((k: string, value: string)) = this.Yield((k, SenseValue.Atom (SenseAtom value)))
            member inline this.Yield((k: string, sense: Sense)) = this.Yield((k, Sense.asValue sense))

    // ----

    type SenseMapBuilder() =
        inherit SenseMapBuilderBase()
        member inline _.Run(kvs: (string * SenseValue) list): SenseMap = SenseMap (Map.ofList kvs)

    let senseMap = SenseMapBuilder()

    // ----

    type SenseBuilder() =
        inherit SenseMapBuilderBase()
        member inline _.Run(kvs: (string * SenseValue) list): Sense = Sense (SenseMap (Map.ofList kvs))

    let sense = SenseBuilder()

    // ----

    let private testSenseMap () =
        senseMap {
            "k1", "v"
            "k2", senseMap {
                ()
            }
            "k3", SenseAtom "a"
            "k4", SenseValue.Atom (SenseAtom "a")
            "k5", sense {
                ()
            }
            yield! senseMap {
                ()
            }
        }

    let private testSense () =
        sense {
            "k1", "v"
            "k2", senseMap {
                ()
            }
            "k2", sense {
                ()
            }
            yield! senseMap {
                ()
            }
        }
