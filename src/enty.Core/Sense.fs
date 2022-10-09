namespace enty.Core

open System
open FsToolkit.ErrorHandling
open enty.Utils

// [<RequireQualifiedAccess>]
// type Sense =
//     | Value of string
//     | Map of Map<string, Sense>
//     | List of Sense list

type SenseValue = SenseValue of string

type SenseList = SenseList of SenseEntry list

and SenseMap = SenseMap of Map<string, SenseEntry>

and [<RequireQualifiedAccess>]
    SenseEntry =
    | Value of SenseValue
    | List of SenseList
    | Map of SenseMap

and Sense = Sense of SenseMap


[<RequireQualifiedAccess>]
module Map =

    let mergeWith (combiner: 'k -> 'v -> 'v -> 'v) (source1: Map<'k, 'v>) (source2: Map<'k, 'v>) : Map<'k ,'v> =
        Map.fold (fun m k v' -> Map.add k (match Map.tryFind k m with Some v -> combiner k v v' | None -> v') m) source1 source2

[<RequireQualifiedAccess>]
module SenseEntry =

    let rec tryGet (path: string list) (entry: SenseEntry) : SenseEntry option =
        match path with
        | key :: tailPath ->
            match entry with
            | SenseEntry.Map (SenseMap map) ->
                option {
                    let! innerValue = map |> Map.tryFind key
                    return! tryGet tailPath innerValue
                }
            | SenseEntry.List (SenseList ls) ->
                option {
                    let! intKey = Int32.TryParse(key) |> Option.ofTryByref
                    let! innerValue = ls |> List.tryItem intKey
                    return! tryGet tailPath innerValue
                }
            | _ -> None
        | [] -> Some entry

    let tryAsValue (sense: SenseEntry) : string option =
        match sense with
        | SenseEntry.Value (SenseValue v) -> Some v
        | _ -> None

    let tryGetValue (path: string list) (entry: SenseEntry) : string option =
        tryGet path entry |> Option.bind tryAsValue

    let tryItem (key: string) (entry: SenseEntry) : SenseEntry option =
        tryGet [key] entry

    let tryAsList (entry: SenseEntry) : SenseEntry list option =
        match entry with
        | SenseEntry.List (SenseList entries) -> Some entries
        | _ -> None

[<RequireQualifiedAccess>]
module Sense =

    let asEntry (sense: Sense) : SenseEntry =
        sense |> function Sense senseMap -> SenseEntry.Map senseMap

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
        member inline _.Source(entries: SenseEntry list) = entries
        member inline _.Yield(entry: SenseEntry) = [entry]
        member inline _.YieldFrom(entries: SenseEntry list) = entries
        member inline _.Zero() = []
        member inline _.Combine(ss1, ss2) = ss1 @ ss2
        member inline _.Delay(f) = f ()
        member inline _.For(sequence: 'a seq, body: 'a -> SenseEntry list) = sequence |> Seq.collect body |> Seq.toList
        member inline _.Run(entries: SenseEntry list) = SenseList entries

    [<AutoOpen>]
    module SenseListBuilderExtensions =
        type SenseListBuilder with
            member inline this.Yield(senseValue: SenseValue) = this.Yield(SenseEntry.Value senseValue)
            member inline this.Yield(senseMap: SenseMap) = this.Yield(SenseEntry.Map senseMap)
            member inline this.Yield(senseList: SenseList) = this.Yield(SenseEntry.List senseList)
            member inline this.Yield(value: string) = this.Yield(SenseValue value)
            member inline this.Yield(sense: Sense) = this.Yield(Sense.asEntry sense)

            member inline this.Source(values: string list) = values |> List.map (SenseValue >> SenseEntry.Value)
            member inline this.Source(senseValues: SenseValue list) = senseValues |> List.map SenseEntry.Value

    let senseList = SenseListBuilder()

    // ----

    [<AbstractClass>]
    type SenseMapBuilderBase() =
        member inline _.Source(kvs: (string * SenseEntry) list) = kvs
        member inline _.Yield((k: string, v: SenseEntry)) = [ (k, v) ]
        member inline _.YieldFrom(kvs: (string * SenseEntry) list) = kvs
        member inline _.Zero() = []
        member inline _.Combine(ss1, ss2) = ss1 @ ss2
        member inline _.Delay(f) = f ()
        member inline _.For(sequence: 'a seq, body: 'a -> (string * Sense) list) = sequence |> Seq.collect body |> Seq.toList

    [<AutoOpen>]
    module SenseMapBuilderBaseExtensions =
        type SenseMapBuilderBase with
            member inline this.Source(SenseMap senseMap) = senseMap |> Map.toList

            member inline this.Yield((k: string, senseValue: SenseValue)) = this.Yield((k, SenseEntry.Value senseValue))
            member inline this.Yield((k: string, senseList: SenseList)) = this.Yield((k, SenseEntry.List senseList))
            member inline this.Yield((k: string, senseMap: SenseMap)) = this.Yield((k, SenseEntry.Map senseMap))
            member inline this.Yield((k: string, value: string)) = this.Yield((k, SenseEntry.Value (SenseValue value)))
            member inline this.Yield((k: string, sense: Sense)) = this.Yield((k, Sense.asEntry sense))

    // ----

    type SenseMapBuilder() =
        inherit SenseMapBuilderBase()
        member inline _.Run(kvs: (string * SenseEntry) list): SenseMap = SenseMap (Map.ofList kvs)

    let senseMap = SenseMapBuilder()

    // ----

    type SenseBuilder() =
        inherit SenseMapBuilderBase()
        member inline _.Run(kvs: (string * SenseEntry) list): Sense = Sense (SenseMap (Map.ofList kvs))

    let sense = SenseBuilder()

    // ----

    let private testSenseMap () =
        senseMap {
            "k1", "v"
            "k2", senseMap {
                ()
            }
            "k3", SenseValue "a"
            "k4", SenseEntry.Value (SenseValue "a")
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
