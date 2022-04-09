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
        let inline isObject (o: JsonValue) : bool = emitJsExpr (o) "$0 === null ? false : (Object.getPrototypeOf($0 || false) === Object.prototype)"

        let rec parseJsonToSense (json: JsonValue) : Sense =
            if isString json then
                Sense.Value !!json
            elif isArray json then
                Seq.ofArray !!json
                |> Seq.map parseJsonToSense
                |> Seq.toList |> Sense.List
            elif isObject json then
                let entries = JS.Constructors.Object.entries(json)
                [ for entry in entries do
                    let key, value = entry?(0), entry?(1)
                    key, parseJsonToSense value ]
                |> Map.ofList
                |> Sense.Map
            else
                invalidOp "JSON isn't any supported type"

    let ofJsObject (o: obj) =
        parseJsonToSense o

    let rec toJsObject (sense: Sense) : obj =
        match sense with
        | Sense.Value value -> !!value
        | Sense.List ls ->
            let arr = ls |> Seq.map toJsObject |> Seq.toArray
            !!JS.Constructors.Array.from(arr)
        | Sense.Map mp ->
            let obj = obj()
            mp |> Map.iter (fun key value ->
                obj?(key) <- toJsObject value
            )
            obj
