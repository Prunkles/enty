namespace Khonsu.ApiSpec.Fable

open Fable.Core
open Thoth.Fetch
open Khonsu.ApiSpec

//module Endpoint =
//
//    open Fetch.Types
//    
//    let buildHttpGet (endpoint: HttpGetEndpoint<'i, 'o>) : 'i -> JS.Promise<'o> =
//        let buildRequest = endpoint.BuildRequestClient
//        let buildResponse = endpoint.BuildResponseClient
//        fun request -> promise {
//            let httpRequest = buildRequest request
//            let url = httpRequest.Url
//            let headers = httpRequest.Headers |> Map.toList |>List.map (fun (k, v) -> HttpRequestHeaders.Custom (k, v))
//            let! fetchResponse = Fetch.get(url, headers=headers)
//            let jsonString = JS.JSON.stringify(fetchResponse)
//            let response = { HttpResponse.Body = jsonString; Headers = Map.empty; StatusCode = 200 }
//            let tResponse = buildResponse response
//            return tResponse
//        }
//    
//    let build (endpoint: Endpoint<'i, 'o>) : 'i -> JS.Promise<'o> =
//        match endpoint with
//        | Endpoint.Http endpoint ->
//            match endpoint with
//            | HttpEndpoint.Get endpoint -> buildHttpGet endpoint
