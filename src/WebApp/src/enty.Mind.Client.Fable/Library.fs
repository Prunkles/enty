namespace enty.Mind.Client.Fable

open System
open Fable.Core
open Fable.Core.JsInterop
open Khonsu.Coding.Json
open Khonsu.Coding.Json.Fable
open enty.Core

open Fetch

type IMindApi =
    abstract Remember: eid: EntityId * senseString: string -> Async<unit>
    abstract Forget: eid: EntityId -> Async<unit>
    abstract Wish: wishString: string * offset: int * limit: int -> Async<EntityId[] * int>
    abstract GetEntities: eids: EntityId[] -> Async<Entity[]>

type FetchMindApi(baseAddress: string) =
    let jsonEncoding = ThothJsonEncoding() :> IJsonEncoding<_>
    let jsonDecoder = ThothJsonDecoding() :> IJsonDecoding<_>
    let fetchR route (request: 'q) encoder =
        promise {
            let url = baseAddress + route
            let jv: 'j = encoder request jsonEncoding
            let bodyString = jsonEncoding.EncodeToString(jv)
            let! fetchResponse =
                fetch url [
                    requestHeaders [
                        HttpRequestHeaders.ContentType "application/json"
                    ]
                    RequestProperties.Method HttpMethod.POST
                    RequestProperties.Body !^bodyString
                ]
            return! fetchResponse.text()
        } |> Async.AwaitPromise
    let mkResponse bodyString decoder : 'p =
        let responseResult = jsonDecoder.DecodeFromString(bodyString, decoder jsonDecoder)
        match responseResult with
        | Ok response -> response
        | Error err -> failwith $"{err}"
    
    interface IMindApi with
        member this.Forget(EntityId eidG) = async {
            fetch (baseAddress + "/forget/" + string eidG) []
            ()
        }
        member this.GetEntities(eids) = async {
            let! rpBodyString = fetchR "/getEntities" request (GetEntitiesRequest.Encoder())
            return mkResponse rpBodyString (GetEntitiesResponse.Decoder<_>())
        }
        member this.Remember(eid, senseString) = async {
            let! rpBodyString = fetchR "/remember" request (RememberRequest.Encoder<_>())
            return ()
        }
        member this.Wish(wishString, offset, limit) = async {
            printfn "wish api"
            let! rpBodyString = fetchR "/wish" request (WishRequest.Encoder())
            return mkResponse rpBodyString (WishResponse.Decoder())
        }
