namespace enty.Core


type Sense =
    | Value of string
    | Map of Map<string, Sense>
    | List of Sense list

[<RequireQualifiedAccess>]
module Sense =

    open System

    let rec tryGet (path: string list) sense =
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
