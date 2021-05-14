namespace enty.Mind.Client.Fable

[<AutoOpen>]
module private Helpers =

    open Fable.Core
    open Fable.Core.JsInterop
    open enty.Core
    
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

[<RequireQualifiedAccess>]
module Sense =
    
    let ofJsObject (o: obj) =
        parseJsonToSense o

module MindService =

    open System
    open Fable.Core
    open Fable.Core.JsInterop
    open Fetch.Types
    open Thoth.Fetch
    open Thoth.Json
    open enty.Core
    
    type Promise<'T> = JS.Promise<'T>
    
    let getSense (entityId: EntityId) : Promise<Sense> = promise {
        let (EntityId entityId) = entityId
        let url = "/api/getSense"
        let data = Encode.object [
            "entityId", Encode.guid entityId
        ]
        let! response = Fetch.post(url, data)
        let sense = Sense.ofJsObject response
        return sense
    }
    
    let wish (wishString: string) (page: int) (pageSize: int) : Promise<EntityId[]> = promise {
        let url = "/api/wish"
        let headers = [ HttpRequestHeaders.ContentType "application/json" ]
        let data = Encode.object [
            "wishString", Encode.string wishString
            "pagination", Encode.object [
                "page", Encode.int page
                "pageSize", Encode.int pageSize
            ]
        ]
        let! response = Fetch.post(url, data, headers=headers)
        printfn $"Response: %A{JS.JSON.stringify response}"
        let ids: Guid[] = response?entityIds
        let entityIds = ids |> Seq.map EntityId |> Seq.toArray
        return entityIds
    }
