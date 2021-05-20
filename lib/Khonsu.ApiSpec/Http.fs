module Khonsu.ApiSpec.Http

type IHttpRequestBody =
    abstract AsString: unit -> string

type IHttpResponseBody =
    abstract AsString: unit -> string

type HttpHeaders = Map<string, string>

type HttpRequest =
    { Verb: string
      Route: string
      Headers: HttpHeaders 
      Body: IHttpRequestBody }

type HttpResponse =
    { Body: IHttpResponseBody
      Headers: HttpHeaders
      StatusCode: int }

// (q -> qR) -> (pR -> p) -> (qR -> pR) -> (q -> p)
//                           ---need---    --gain--
type IClientCoders<'q, 'p, 'qR, 'pR> =
    abstract DecodeResponse: 'pR -> 'p
    abstract EncodeRequest: 'q -> 'qR

// (qR -> q) -> (p -> pR) -> (q -> p) -> (qR -> pR)
//                           --need--    ---gain---
type IServerCoders<'q, 'p, 'qR, 'pR> =
    abstract DecodeRequest: 'qR -> 'q
    abstract EncodeResponse: 'p -> 'pR

type ICoders<'q, 'p, 'qR, 'pR> =
    inherit IServerCoders<'q, 'p, 'qR, 'pR>
    inherit IClientCoders<'q, 'p, 'qR, 'pR>

//type HttpGetEndpoint<'q, 'p> = { Coders: ICoders<'q, 'p, HttpGetRequest, HttpResponse> }
//type HttpPostEndpoint<'q, 'p> = { Coders: ICoders<'q, 'p, HttpPostRequest, HttpResponse> }

[<RequireQualifiedAccess>]
type HttpEndpoint<'q, 'p> =
//    | Get of HttpGetEndpoint<'q, 'p>
//    | Post of HttpPostEndpoint<'q, 'p>
    { Coders: ICoders<'q, 'p, HttpRequest, HttpResponse> }


//module HttpEndpoint =
//    
//    let choose criteria (endpoints: HttpEndpoint<_, _> seq) : HttpEndpoint<_, _> =
//        let coders =
//            { new ICoders<_, _, _, _> with
//                
//            }
//        ()