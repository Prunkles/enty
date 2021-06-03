module enty.Mind.Client.Fable.SenseJsObject

open enty.Core

[<RequireQualifiedAccess>]
module Sense =

    open Fable.Core
    open Fable.Core.JsInterop

    [<AutoOpen>]
    module private Helpers =

        type JsonValue = obj

        let inline isString (o: JsonValue) : bool = o :? string
        let inline isArray (o: JsonValue) : bool = JS.Constructors.Array.isArray(o)

        let rec parseJsonToSense (json: JsonValue) : Sense =
            if isString json then
                Sense.Value !!json
            elif isArray json then
                Seq.ofArray !!json
                |> Seq.map parseJsonToSense
                |> Seq.toList |> Sense.List
            else
                invalidOp ""

    let ofJsObject (o: obj) =
        parseJsonToSense o

    let rec toJsObject (sense: Sense) : obj =
        match sense with
        | Sense.Value value -> !!value
        | Sense.List ls ->
            let arr = ls |> Seq.map toJsObject |> Seq.toArray
            !!JS.Constructors.Array.from(arr)
