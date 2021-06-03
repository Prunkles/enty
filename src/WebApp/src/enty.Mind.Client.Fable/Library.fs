namespace enty.Mind.Client.Fable

open System
open Fable.Core
open Fable.Core.JsInterop
open Khonsu.Coding.Json
open Khonsu.Coding.Json.Fable
open enty.Core
open SenseJsObject

open Fetch

module AsyncBuilderPromiseExtensions =
    open Fable.Core
    type AsyncBuilder with
        member this.ReturnFrom(x: JS.Promise<'a>): Async<'a> = Async.AwaitPromise(x)
        member this.Bind(x: JS.Promise<'a>, f: 'a -> Async<'b>): Async<'b> = this.Bind(Async.AwaitPromise x, f)

open AsyncBuilderPromiseExtensions

type IMindApi =
    abstract Remember: eid: EntityId * senseString: string -> Async<unit>
    abstract Forget: eid: EntityId -> Async<unit>
    abstract Wish: wishString: string * offset: int * limit: int -> Async<EntityId[] * int>
    abstract GetEntities: eids: EntityId[] -> Async<Entity[]>

type FetchMindApi(baseAddress: string) =
    let jsonEncoding = ThothJsonEncoding() :> IJsonEncoding<_>
    let jsonDecoding = ThothJsonDecoding() :> IJsonDecoding<_>

    interface IMindApi with
        member this.Forget(EntityId eidG) = async {
            let! response = fetch (baseAddress + "/forget/" + string eidG) [
                RequestProperties.Method HttpMethod.POST
            ]
            if not response.Ok then failwithf "%A" response
            return ()
        }
        member this.GetEntities(eids) = async {
            let requestBodyString =
                let r = JsonAEncode.object [
                    "eids", JsonAEncode.array (Array.map (EntityId.Unwrap >> JsonAEncode.guid) eids)
                ]
                jsonEncoding.EncodeToString(r jsonEncoding)
            let! response = fetch (baseAddress + "/getEntities") [
                RequestProperties.Method HttpMethod.POST
                requestHeaders [
                    HttpRequestHeaders.ContentType "application/json"
                ]
                RequestProperties.Body !^requestBodyString
            ]
            if not response.Ok then return failwithf "%A" response else
            let! responseBodyString = response.text()
            let decodeResult =
                let decodeEntity = jsonADecoder {
                    let! id = JsonADecode.field "id" JsonADecode.guid
                    and! sense = JsonADecode.field "sense" JsonADecode.raw
                    return { Id = EntityId id; Sense = Sense.ofJsObject sense }
                }
                let decoder = JsonADecode.field "entities" (JsonADecode.array decodeEntity)
                jsonDecoding.DecodeFromString(responseBodyString, decoder jsonDecoding)
            let entities = decodeResult |> function Ok x -> x | Error err -> failwithf "%A" err
            return entities
        }
        member this.Remember(eid, senseString) = async {
            let requestBodyString =
                let encoded = JsonAEncode.object [
                    "senseString", JsonAEncode.string senseString
                ]
                jsonEncoding.EncodeToString(encoded jsonEncoding)
            let url = sprintf "%s/remember/%s" baseAddress (eid |> EntityId.Unwrap |> string)
            let! response = fetch url [
                RequestProperties.Method HttpMethod.POST
                requestHeaders [
                    HttpRequestHeaders.ContentType "application/json"
                ]
                RequestProperties.Body !^requestBodyString
            ]
            if not response.Ok then return failwithf "%A" response else
            return ()
        }
        member this.Wish(wishString, offset, limit) = async {
            let requestBodyString =
                let encoded = JsonAEncode.object [
                    "wishString", JsonAEncode.string wishString
                ]
                jsonEncoding.EncodeToString(encoded jsonEncoding)
            let url = sprintf "%s/wish?offset=%i&limit=%i" baseAddress offset limit
            let! response = fetch url [
                RequestProperties.Method HttpMethod.POST
                requestHeaders [
                    HttpRequestHeaders.ContentType "application/json"
                ]
                RequestProperties.Body !^requestBodyString
            ]
            if not response.Ok then return failwith "%A" response else
            let! responseBodyString = response.text()
            let decodeResult =
                let decoder = jsonADecoder {
                    let! eidGs = JsonADecode.field "eids" (JsonADecode.array JsonADecode.guid)
                    let! total = JsonADecode.field "total" JsonADecode.int
                    return Array.map EntityId eidGs, total
                }
                jsonDecoding.DecodeFromString(responseBodyString, decoder jsonDecoding)
            let eids, total = decodeResult |> function Ok x -> x | Error err -> failwith "%A" err
            return eids, total
        }
