namespace enty.Mind.Client.Fable

open Fable.Core
open Fable.Core.JsInterop
open Fable.SimpleHttp
open Fetch
open Khonsu.Coding.Json
open Khonsu.Coding.Json.Fable

open enty.Core
open SenseJsObject


[<AutoOpen>]
module AsyncBuilderPromiseExtensions =
    type AsyncBuilder with
        member this.Source(a: Async<'a>): Async<'a> = a
        member this.Source(p: JS.Promise<'a>): Async<'a> = Async.AwaitPromise(p)

[<RequireQualifiedAccess>]
type WishOrderingKey =
    | ByCreation
    | ByUpdated
    | ById

type WishOrdering =
    { Descending: bool
      Key: WishOrderingKey }

type IMindApi =
    abstract Remember: eid: EntityId * senseString: string -> Async<Result<unit, string>>
    abstract Forget: eid: EntityId -> Async<unit>
    abstract Wish: wishString: string * ordering: WishOrdering * offset: int * limit: int -> Async<Result<EntityId[] * int, string>>
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
            let! response =
                Http.request url
                |> Http.method Fable.SimpleHttp.HttpMethod.POST
                |> Http.header (Header ("Content-Type", "application/json"))
                |> Http.content (BodyContent.Text requestBodyString)
                |> Http.send
            if response.statusCode <> 200 then
                return Error $"Failed remember: {response.responseText}"
            else
                return Ok ()
        }
        member this.Wish(wishString, ordering, offset, limit) = async {
            let requestBodyString =
                let encoded = JsonAEncode.object [
                    "wishString", JsonAEncode.string wishString
                    "ordering", JsonAEncode.object [
                        "key", JsonAEncode.string <| match ordering.Key with WishOrderingKey.ByCreation -> "ByCreation" | WishOrderingKey.ByUpdated -> "ByUpdated" | WishOrderingKey.ById -> "ById"
                        "descending", JsonAEncode.bool ordering.Descending
                    ]
                ]
                jsonEncoding.EncodeToString(encoded jsonEncoding)
            let url = $"{baseAddress}/wish?offset={offset}&limit={limit}"
            let! response =
                Http.request url
                |> Http.method Fable.SimpleHttp.HttpMethod.POST
                |> Http.content (BodyContent.Text requestBodyString)
                |> Http.header (Header ("Content-Type", "application/json"))
                |> Http.send
            if response.statusCode = 200 then
                let responseBodyString = response.responseText
                let decodeResult =
                    let decoder = jsonADecoder {
                        let! eidGs = JsonADecode.field "eids" (JsonADecode.array JsonADecode.guid)
                        let! total = JsonADecode.field "total" JsonADecode.int
                        return Array.map EntityId eidGs, total
                    }
                    jsonDecoding.DecodeFromString(responseBodyString, decoder jsonDecoding)
                let eids, total = decodeResult |> function Ok x -> x | Error err -> failwithf "%A" err
                return Ok (eids, total)
            elif response.statusCode = 400 then
                let responseBodyString = response.responseText
                return Error responseBodyString
            else
                return failwith $"Unexpected response: %A{response}"
        }
