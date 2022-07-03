namespace enty.Core

open System
open FsToolkit.ErrorHandling
open enty.Utils

[<RequireQualifiedAccess>]
type Sense =
    | Value of string
    | Map of Map<string, Sense>
    | List of Sense list


module Map =
    let mergeWith combiner (source1: Map<'Key, 'Value>) (source2: Map<'Key, 'Value>) =
        Map.fold (fun m k v' -> Map.add k (match Map.tryFind k m with Some v -> combiner v v' | None -> v') m) source1 source2


[<RequireQualifiedAccess>]
module Sense =

    // let rec tryMerge (sense1: Sense) (sense2: Sense) : Sense option =
    //     match sense1, sense2 with
    //     | Sense.List ls1, Sense.List ls2 ->
    //         Some ^ Sense.List (ls1 @ ls2)
    //     | Sense.Map mp1, Sense.Map mp2 ->
    //         // TODO: Merge fields
    //         Some ^ Sense.Map (Map.toList mp1 @ Map.toList mp2 |> Map.ofList)
    //     | _ -> None

    let empty () : Sense =
        Sense.Map Map.empty

    let rec merge (sense1: Sense) (sense2: Sense) : Sense =
        match sense1, sense2 with
        | Sense.List ls1, Sense.List ls2 ->
            Sense.List (ls1 @ ls2)
        | Sense.Map mp1, Sense.Map mp2 ->
            Sense.Map ^ Map.mergeWith merge mp1 mp2
        | (Sense.Value _ as vl1), (Sense.Value _ as vl2) -> Sense.List [ vl1; vl2 ]
        | _ -> invalidOp $"Cannot merge {sense1} and {sense2}"

    let rec tryGet (path: string list) sense : Sense option =
        match path with
        | key :: tailPath ->
            match sense with
            | Sense.Map map ->
                option {
                    let! innerValue = map |> Map.tryFind key
                    return! tryGet tailPath innerValue
                }
            | Sense.List ls ->
                option {
                    let! intKey = Int32.TryParse(key) |> Option.ofTryByref
                    let! innerValue = ls |> List.tryItem intKey
                    return! tryGet tailPath innerValue
                }
            | _ -> None
        | [] -> Some sense

    let tryGetValue path sense =
        match tryGet path sense with
        | Some (Sense.Value v) -> Some v
        | _ -> None

    let tryItem (key: string) (sense: Sense) : Sense option =
        match sense with
        | Sense.Map map -> map |> Map.tryFind key
        | _ -> None

    let tryAsList (sense: Sense) : Sense list option =
        match sense with
        | Sense.List xs -> Some xs
        | _ -> None

    let tryAsValue sense = match sense with Sense.Value v -> Some v | _ -> None

[<AutoOpen>]
module Builders =

    type SenseListBuilder() =
        member _.Yield(value: string) = [Sense.Value value]
        member _.Yield(sense: Sense) = [sense]
        member _.YieldFrom(values: string list) = values |> List.map Sense.Value
        member _.YieldFrom(senses: Sense list) = senses
        member _.For(sequence: 'a seq, body: 'a -> Sense list) = sequence |> Seq.collect body |> Seq.toList
        member _.Zero() = []
        member _.Combine(ss1, ss2) = ss1 @ ss2
        member _.Delay(f) = f()
        member _.Run(senses: Sense list) = Sense.List senses

    let senseList = SenseListBuilder()

    type SenseMapBuilder() =
        member _.Yield((k: string, v: Sense)) = [ (k, v) ]
        member _.Yield((k: string, v: string)) = [ (k, Sense.Value v) ]
        member _.Zero() = []
        member _.For(sequence: 'a seq, body: 'a -> (string * Sense) list) = sequence |> Seq.collect body |> Seq.toList
        member _.Combine(ss1, ss2) = ss1 @ ss2
        member _.Delay(f) = f()
        member _.Run(props: (string * Sense) list): Sense = Sense.Map (Map.ofList props)

    let senseMap = SenseMapBuilder()
