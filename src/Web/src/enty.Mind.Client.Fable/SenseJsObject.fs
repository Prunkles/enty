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

        let rec private parseJsonToSenseValue (json: JsonValue) : SenseValue =
            if isString json then
                SenseValue.atom !!json
            elif isArray json then
                Seq.ofArray !!json
                |> Seq.map parseJsonToSenseValue
                |> Seq.toList
                |> SenseValue.list
            elif isObject json then
                let entries = JS.Constructors.Object.entries(json)
                [ for entry in entries do
                    let key, value = entry?(0), entry?(1)
                    key, parseJsonToSenseValue value ]
                |> Map.ofList
                |> SenseValue.map
            else
                invalidOp "JSON isn't any supported type"

        let rec parseJsonToSense (json: JsonValue) : Result<Sense, string> =
            parseJsonToSenseValue json |> Sense.parseSenseValue

    let parseJsObject (o: obj) : Result<Sense, string> =
        parseJsonToSense o

    let rec private senseValueToJsObject (senseValue: SenseValue) : obj =
        match senseValue with
        | SenseValue.Atom (SenseAtom value) -> !!value
        | SenseValue.List (SenseList ls) ->
            let arr = ls |> Seq.map senseValueToJsObject |> Seq.toArray
            !!JS.Constructors.Array.from(arr)
        | SenseValue.Map (SenseMap mp) ->
            let obj = obj()
            mp |> Map.iter (fun key value ->
                obj?(key) <- senseValueToJsObject value
            )
            obj

    let rec toJsObject (sense: Sense) : obj =
        sense |> Sense.asValue |> senseValueToJsObject
