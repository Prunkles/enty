module enty.Mind.Gateway.Endpoints

open Microsoft.Extensions.Logging
open Giraffe
open Giraffe.EndpointRouting
open Khonsu.Coding.Json
open Khonsu.Coding.Json.Net
open enty.Utils
open enty.Core
open enty.Mind
open enty.Mind.Client
open WishParsing
open SenseParsing
open SenseJToken


let wishHandler : HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()
    let offset = ctx.TryGetQueryStringValue("offset") |> Option.defaultValue "0" |> int
    let limit = ctx.TryGetQueryStringValue("limit") |> Option.defaultValue "16" |> int
    if limit > 64 then return! RequestErrors.BAD_REQUEST "limit > 64" next ctx else
    let! jsonRequest = ctx.BindJsonAsync<{| wishString: string |}>()
    let wishString = jsonRequest.wishString
    let wish = Wish.parse wishString
    match wish with
    | Ok wish ->
        let! eids, total = mindService.Wish(wish, offset, limit)
        let eidGs = eids |> Array.map EntityId.Unwrap
        return! ctx.WriteJsonAsync({| eids = eidGs; total = total |})
    | Error reason ->
        return! RequestErrors.BAD_REQUEST $"Invalid wish: {reason}" next ctx
}

let rememberHandler eidG : HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()
    let eid = EntityId eidG
    let! requestBodyString = ctx.ReadBodyFromRequestAsync()
    ctx.GetLogger().LogDebug($"BodyString:\n{requestBodyString}")
    let json = JsonValue.Parse(requestBodyString)
//    let decode = JsonADecode.field "sense" JsonADecode.raw (ThothJsonDecoding())
//    let jsonSense = decode json |> function Ok x -> x | Error err -> failwithf "%A" err
//    let sense = Sense.ofJToken jsonSense
    let decoder = JsonADecode.field "senseString" JsonADecode.string (ThothJsonDecoding())
    let senseString = decoder json |> Result.getOk
    let sense = Sense.parse senseString |> Result.getOk
    do! mindService.Remember(eid, sense)
    return! Successful.OK "" next ctx
}

let forgetHandler eidG : HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()
    let eid = EntityId eidG
    do! mindService.Forget(eid)
    return! Successful.OK "" next ctx
}

let getEntitiesHandler : HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()
    let! requestBodyString = ctx.ReadBodyFromRequestAsync()
    let eidGs =
        let decoding: IJsonDecoding<_> = upcast ThothJsonDecoding()
        let decoder = JsonADecode.field "eids" (JsonADecode.array JsonADecode.guid) decoding
        decoding.DecodeFromString(requestBodyString, decoder)
        |> function Ok x -> x | Error err -> failwithf "%A" err
    let eids = eidGs |> Array.map EntityId

    let! entities = mindService.GetEntities(eids)

    let r =
        let encoding: IJsonEncoding<_> = upcast ThothJsonEncoding()
        let encodeEntity (entity: Entity) =
            JsonAEncode.object [
                "id", JsonAEncode.guid (EntityId.Unwrap entity.Id)
                "sense", JsonAEncode.raw (Sense.toJToken entity.Sense)
            ]
        let encoder = JsonAEncode.object [ "entities", JsonAEncode.array (entities |> Array.map encodeEntity) ]
        let json = encoder encoding
        encoding.EncodeToString(json)

    do ctx.SetContentType("application/json")
    return! ctx.WriteStringAsync(r)
}

let endpoints = [
    POST [
        route "/wish" wishHandler
        routef "/remember/%O" rememberHandler
        routef "/forget/%O" forgetHandler
        route "/getEntities" getEntitiesHandler
    ]
]

let notFoundHandler: HttpHandler =
    "Not found" |> text |> RequestErrors.notFound
