namespace enty.Mind.Client.Fable

open System
open Fable.Core
open Fable.Core.JsInterop
open Khonsu.Coding.Json
open Khonsu.Coding.Json.Fable
open enty.Core
open enty.Mind.Server.Api

open Fetch

type FetchMindApi() =
    let jsonEncoding = ThothJsonEncoding() :> IJsonEncoding<_>
    let jsonDecoder = ThothJsonDecoding() :> IJsonDecoding<_>
    let baseRoute = "/mind"
    let fetchR route (request: 'q) encoder =
        promise {
            let url = baseRoute + route
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
    
    interface IMindApi<JsonValue> with
        member this.Forget(request) = async {
            let! rpBodyString = fetchR "/forget" request (ForgetRequest.Encoder())
            return ()
        }
        member this.GetEntities(request) = async {
            let! rpBodyString = fetchR "/getEntities" request (GetEntitiesRequest.Encoder())
            return mkResponse rpBodyString (GetEntitiesResponse.Decoder<_>())
        }
        member this.Remember(request) = async {
            let! rpBodyString = fetchR "/remember" request (RememberRequest.Encoder<_>())
            return ()
        }
        member this.Wish(request) = async {
            printfn "wish api"
            let! rpBodyString = fetchR "/wish" request (WishRequest.Encoder())
            return mkResponse rpBodyString (WishResponse.Decoder())
        }
