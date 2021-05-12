module enty.Mind.Server.SenseJson

open System
open enty.Core

[<RequireQualifiedAccess>]
module Sense =

    open FSharp.Data

    let rec ofJson (json: JsonValue) : Sense =
        match json with
        | JsonValue.Record properties ->
            properties
            |> Seq.map (fun (key, innerJson) -> key, ofJson innerJson)
            |> Map.ofSeq
            |> Sense.Map
        | JsonValue.Array elements ->
            elements
            |> Seq.map ofJson
            |> Seq.toList
            |> Sense.List
        | JsonValue.String s -> Sense.Value s
        | _ -> NotSupportedException() |> raise
    
    let rec toJson (value: Sense) : JsonValue =
        match value with
        | Sense.Value s -> JsonValue.String s
        | Sense.Map m ->
            m
            |> Seq.map (fun (KeyValue(k, v)) -> k, toJson v)
            |> Seq.toArray
            |> JsonValue.Record
        | Sense.List l ->
            l
            |> Seq.map toJson
            |> Seq.toArray
            |> JsonValue.Array
