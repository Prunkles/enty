namespace enty.WebApp

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
        let inline isObject (o: JsonValue) : bool =
            emitJsExpr (o) "$0 === null ? false : (Object.getPrototypeOf($0 || false) === Object.prototype)"
        
        let objectEntries (o: obj) : (string * obj) seq =
            emitJsExpr (o) "Object.entries($0)"

        let rec parseJsonToSense (json: JsonValue) : Sense =
            if isString json then
                Sense.Value !!json
            elif isObject json then
                objectEntries json
                |> Seq.map (fun (key, value) ->
                    key, parseJsonToSense value
                )
                |> Map.ofSeq |> Sense.Map
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
        | Sense.Map mp ->
            let o = obj()
            for KeyValue(k, v) in mp do
                o?(k) <- toJsObject v
            !!o

module MindServiceImpl =

    open enty.Mind.Client.Fable
    open enty.Mind.Server.Api
    
    let mindService: IMindService =
        let decodeSense (SenseDto j) = Sense.ofJsObject j
        let encodeSense sense = SenseDto (Sense.toJsObject sense)
        upcast ApiMindService(FetchMindApi(), encodeSense, decodeSense)
