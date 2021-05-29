[<System.ObsoleteAttribute>]
module enty.Core.Exp


//[<RequireQualifiedAccess>]
//type Sense =
//    | List of Sense list
//    | Value of string
//
//
//module Sense =
//    
//    let asValue sense = match sense with Sense.Value v -> Some v | _ -> None
//    
//    let (|Map|_|) sense =
//        match sense with
//        | Sense.List ls ->
//            let folder (state: Map<Sense, Sense> option) (sense: Sense) : Map<_, _> option =
//                match state with
//                | None -> None
//                | Some mp ->
//                    match sense with
//                    | Sense.List [ k; v ] ->
//                        mp |> Map.add k v |> Some
//                    | _ -> None
//            ls |> List.fold folder (Some Map.empty)
//        | _ -> None
//
//    let ofMap (mp: Map<Sense, Sense>) : Sense =
//        mp |> Seq.map (fun (KeyValue(k, v)) -> Sense.List [ k; v ]) |> Seq.toList |> Sense.List
//    
//    let rec byMapPath (path: string list) sense : Sense option =
//        match path, sense with
//        | [], _ -> Some sense
//        | k::tail, Map mp ->
//            match Map.tryFind (Sense.Value k) mp with
//            | None -> None
//            | Some v -> byMapPath tail v
//        | _ -> None
//
//
//[<AutoOpen>]
//module Builder =
//    
//    type SenseListBuilder() =
//        member _.Yield(value: string) = [Sense.Value value]
//        member _.Yield(sense: Sense) = [sense]
//        member _.YieldFrom(values: string list) = values |> List.map Sense.Value
//        member _.YieldFrom(senses: Sense list) = senses
//        member _.Zero() = []
//        member _.Combine(ss1, ss2) = ss1 @ ss2
//        member _.Delay(f) = f()
//        member _.Run(senses: Sense list) = Sense.List senses
//    
//    let senseList = SenseListBuilder()
//    
//    type SenseMapBuilder() =
//        member _.Yield((k: Sense, v: Sense)) = [[k; v]]
//        member _.Yield((k: string, v: Sense)) = [[Sense.Value k; v]]
//        member _.Yield((k: string, v: string)) = [[Sense.Value k; Sense.Value v]]
//        member _.Zero() = []
//        member _.Combine(ss1, ss2) = ss1 @ ss2
//        member _.Delay(f) = f()
//        member _.Run(props: Sense list list): Sense = props |> List.map Sense.List |> Sense.List
//    
//    let senseMap = SenseMapBuilder()