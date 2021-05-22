namespace global

//module enty.Mind.Server.Api.Endpoints
//
//open System
//open Khonsu.ApiSpec.Http
//open Khonsu.Coding
//open Khonsu.Coding.Json
//
//// WIP
//
//module Coders =
//    
//    let create decodeResponse encodeRequest decodeRequest encodeResponse =
//        { new ICoders<_, _, _, _> with
//            member _.DecodeResponse(x) = decodeResponse x
//            member _.EncodeRequest(x) = encodeRequest x
//            member _.DecodeRequest(x) = decodeRequest x
//            member _.EncodeResponse(x) = encodeResponse x }
//
//let wish (jsonDecoding: IJsonDecoding<'JsonValue>) (jsonEncoding: IJsonEncoding<'JsonValue>)
//        : HttpEndpoint<WishRequest, WishResponse<'JsonValue>> =
//    {
//        HttpEndpoint.Coders =
//            Coders.create
//                (fun httpRp ->
//                    let bodyString = httpRp.Body.AsString()
//                    let aDecoder = WishResponse<_>.Decode()
//                    let decoder = aDecoder jsonDecoding
//                    let responseResult = jsonDecoding.DecodeFromString(bodyString, decoder)
//                    match responseResult with
//                    | Ok response -> response
//                    | Error err -> failwith ""
//                )
//                (fun rq ->
//                    let aRqEncoder = WishRequest.Encode(rq)
//                    let bodyString = jsonEncoding.EncodeToString(aRqEncoder jsonEncoding)
//                    let body = { new IHttpRequestBody with member _.AsString() = bodyString }
//                    let httpRequest =
//                        { HttpRequest.Route = "/wish"
//                          Verb = "POST"
//                          Headers = Map.empty
//                          Body = body }
//                    httpRequest
//                )
//                (fun httpRq ->
//                    match httpRq with
//                    | { Route = "/wish"; Verb = "POST"; Body = body } ->
//                        let aRqDecoder = WishRequest.Decode()
//                        let rqDecoder = aRqDecoder jsonDecoding
//                        let bodyString = body.AsString()
//                        let rqResult = jsonDecoding.DecodeFromString(bodyString, rqDecoder)
//                        match rqResult with
//                        | Ok rq -> rq
//                        | Error err -> failwith ""
//                    | _ -> failwith ""
//                )
//                (fun rp ->
//                    let aRpEncoder = WishResponse<_>.Encode(rp)
//                    let bodyString = jsonEncoding.EncodeToString(aRpEncoder jsonEncoding)
//                    let body = { new IHttpResponseBody with member _.AsString() = bodyString }
//                    let httpRp =
//                        { HttpResponse.StatusCode = 200
//                          Body = body
//                          Headers = Map.empty }
//                    httpRp
//                )
//    }
