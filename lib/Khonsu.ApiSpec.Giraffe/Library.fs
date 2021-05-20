namespace Khonsu.ApiSpec.Giraffe

open Giraffe
open FSharp.Control.Tasks.V2
open System.Threading.Tasks
open Khonsu.ApiSpec
open Microsoft.AspNetCore.Http

module Endpoint =

    open Khonsu.ApiSpec.Http
    open Microsoft.AspNetCore.Http.Extensions
    open Microsoft.Extensions.Primitives
    
    type AspHttpRequest = Microsoft.AspNetCore.Http.HttpRequest
    type AspHttpResponse = Microsoft.AspNetCore.Http.HttpResponse
    
    let readHttpRequest (aspHttpRequest: AspHttpRequest) : Task<HttpRequest> = task {
        let! bodyString = aspHttpRequest.HttpContext.ReadBodyFromRequestAsync()
        let body = { new IHttpRequestBody with member _.AsString() = bodyString }
        return
            { Verb = aspHttpRequest.Method
              Route = UriHelper.GetDisplayUrl(aspHttpRequest)
              Headers = Map.empty
              Body = body }
    }
    
    let writeHttpResponse (httpResponse: HttpResponse) (aspHttpResponse: AspHttpResponse) : Task<unit> = task {
        return failwith ""
    }
    
    let build (endpoint: HttpEndpoint<'q, 'p>) (handler: 'q -> Task<'p>) : HttpHandler =
        let decodeRequest = endpoint.Coders.DecodeRequest
        let encodeResponse = endpoint.Coders.EncodeResponse
        
        fun next ctx -> task {
            let aspHttpRequest = ctx.Request
            let! httpRequest = readHttpRequest aspHttpRequest
            let request = decodeRequest httpRequest
            let! response = handler request
            let httpResponse = encodeResponse response
            let aspHttpResponse = ctx.Response
            do! writeHttpResponse httpResponse aspHttpResponse
            return! earlyReturn ctx
        }
