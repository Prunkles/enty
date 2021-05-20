module enty.Mind.Server.HttpHandlers

open System
open System.Net.Mime
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Khonsu.Coding.Json
open Microsoft.Extensions.Logging
open Giraffe

open enty.Core
open enty.Mind.Server.Api

type JsonValue = Newtonsoft.Json.Linq.JToken


let decodeRequestHandler decoder cont : HttpHandler = fun next ctx -> task {
    let jsonDecoding = ctx.GetService<IJsonDecoding<JsonValue>>()
    let! bodyString = ctx.ReadBodyFromRequestAsync()
    let result = jsonDecoding.DecodeFromString(bodyString, decoder jsonDecoding)
    match result with
    | Ok rq -> return! cont rq next ctx
    | Error err -> return! RequestErrors.BAD_REQUEST err next ctx
}

let encodeResponseHandler rp (encoder: JsonAEncoder<_, _>) : HttpHandler = fun next ctx -> task {
    let jsonEncoding = ctx.GetService<IJsonEncoding<JsonValue>>()
    let bodyString = jsonEncoding.EncodeToString(encoder rp jsonEncoding)
    ctx.SetContentType(MediaTypeNames.Application.Json)
    return! ctx.WriteStringAsync(bodyString)
}


let rememberHandler: HttpHandler =
    let decoder = RememberRequest.Decoder<_>()
    decodeRequestHandler decoder (fun request -> fun next ctx -> task {
        let mindApi = ctx.GetService<IMindApi<JsonValue>>()
        do! mindApi.Remember(request)
        return! earlyReturn ctx
    })

let forgetHandler: HttpHandler =
    let decoder = ForgetRequest.Decoder()
    decodeRequestHandler decoder (fun request -> fun next ctx -> task {
        let mindApi = ctx.GetService<IMindApi<JsonValue>>()
        do! mindApi.Forget(request)
        return! earlyReturn ctx
    })

let wishHandler: HttpHandler =
    let decoder = WishRequest.Decoder()
    decodeRequestHandler decoder (fun request -> fun next ctx -> task {
        let mindApi = ctx.GetService<IMindApi<JsonValue>>()
        let encoder = WishResponse.Encoder()
        let! response = mindApi.Wish(request)
        return! encodeResponseHandler response encoder next ctx
    })

let getEntitiesHandler: HttpHandler =
    let decoder= GetEntitiesRequest.Decoder()
    decodeRequestHandler decoder (fun request -> fun next ctx -> task {
        let mindApi = ctx.GetService<IMindApi<JsonValue>>()
        let encoder = GetEntitiesResponse.Encoder<_>()
        let! response = mindApi.GetEntities(request)
        return! encodeResponseHandler response encoder next ctx
    })



let errorHandler (ex: exn) (logger: ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse
    >=> ServerErrors.INTERNAL_ERROR ex.Message

let server: HttpHandler =
    choose [
        POST >=> route "/remember" >=> rememberHandler
        POST >=> route "/forget" >=> forgetHandler
        POST >=> route "/getEntities" >=> getEntitiesHandler
        POST >=> route "/wish" >=> wishHandler
        RequestErrors.NOT_FOUND "Not found"
    ]
