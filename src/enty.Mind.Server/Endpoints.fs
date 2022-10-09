module enty.Mind.Server.Endpoints

open Microsoft.Extensions.Logging
open Giraffe
open Giraffe.EndpointRouting
open Khonsu.Coding.Json
open Khonsu.Coding.Json.Net

open Thoth.Json.Net
open enty.Utils
open enty.Core
open enty.Core.Parsing.SenseParsing
open enty.Core.Parsing.WishParsing
open enty.Mind.Server.SenseJToken


let wishHandler : HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()

    let offset = ctx.TryGetQueryStringValue("offset") |> Option.defaultValue "0" |> int
    let limit = ctx.TryGetQueryStringValue("limit") |> Option.defaultValue "16" |> int
    if limit > 64 then return! RequestErrors.BAD_REQUEST "limit > 64" next ctx else

    let decode =
        Decode.object ^fun get ->
            let wishString = get.Required.Field "wishString" Decode.string
            let ordering =
                get.Optional.Field "ordering" ^ Decode.object ^fun get ->
                    let key =
                        get.Required.Field "key" (
                            Decode.string
                            |> Decode.andThen ^function
                                | "ById" -> Decode.succeed WishOrderingKey.ById
                                | "ByCreation" -> Decode.succeed WishOrderingKey.ByCreation
                                | "ByUpdated" -> Decode.succeed WishOrderingKey.ByUpdated
                                | invalid -> Decode.fail $"Invalid key: {invalid}"
                        )
                    let descending = get.Required.Field "descending" Decode.bool
                    { Key = key; Descending = descending }
            wishString, ordering

    let! body = ctx.ReadBodyFromRequestAsync()
    match Decode.fromString decode body with
    | Ok (wishString, ordering) ->
        let ordering = ordering |> Option.defaultValue { Key = WishOrderingKey.ByUpdated; Descending = false }
        let wish = Wish.parse wishString
        match wish with
        | Ok wish ->
            let! eids, total = mindService.Wish(wish, ordering, offset, limit)
            let eidGs = eids |> Array.map EntityId.Unwrap
            return! ctx.WriteJsonAsync({| eids = eidGs; total = total |})

        | Error reason ->
            return! RequestErrors.BAD_REQUEST $"Invalid wish: {reason}" next ctx
    | Error error ->
        return! RequestErrors.BAD_REQUEST $"{error}" next ctx
}

let rememberHandler eidG : HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()
    let eid = EntityId eidG
    let! requestBodyString = ctx.ReadBodyFromRequestAsync()
    ctx.GetLogger().LogDebug($"BodyString:\n{requestBodyString}")
    let json = JsonValue.Parse(requestBodyString)
    let decoder = JsonADecode.field "senseString" JsonADecode.string (ThothJsonDecoding())
    let senseString = decoder json
    match senseString with
    | Error reason ->
        return! RequestErrors.BAD_REQUEST $"Invalid request schema: {reason}" next ctx
    | Ok senseString ->
        let sense = Sense.parse senseString
        match sense with
        | Error reason ->
            return! RequestErrors.BAD_REQUEST $"Invalid sense: {reason}" next ctx
        | Ok sense ->
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
