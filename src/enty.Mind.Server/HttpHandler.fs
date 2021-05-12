module enty.Mind.Server.HttpHandler

open System
open FSharp.Data
open Giraffe
open FSharp.Control.Tasks.V2

open Microsoft.AspNetCore.Http
open enty.Core
open enty.Mind
open enty.Mind.Server.Api
open SenseJson


let parseAddRequest (ctx: HttpContext) = task {
    let! requestBody = ctx.ReadBodyFromRequestAsync()
    match JsonValue.Parse(requestBody) with
    | JsonValue.Record [| "EntityId", JsonValue.String entityId; "Sense", sense |] ->
        return
            { EntityId = Guid.Parse(entityId)
              Sense = sense }
    | _ -> return invalidArg "" ""
}

let add: HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()

    let! addRequest = parseAddRequest ctx
    
    let entityId = EntityId addRequest.EntityId
    let sense = Sense.ofJson addRequest.Sense
    
    do! mindService.Remember(entityId, sense)
    
    return! next ctx
}

let remove: HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()
    let! (removeRequest: RemoveRequest) = ctx.BindJsonAsync()
    let entityId = removeRequest.EntityId |> EntityId
    do! mindService.Forget(entityId)
    return! next ctx
}

let getSense: HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()
    let! (getSenseRequest: GetSenseRequest) = ctx.BindJsonAsync()
    let entityId = getSenseRequest.EntityId |> EntityId
    let! sense = mindService.GetSense(entityId)
    let jsonValue = sense |> Sense.toJson
    let json = jsonValue.ToString()
    let! _ = ctx.WriteStringAsync(json)
    do ctx.SetContentType("application/json")
    return! next ctx
}

let app: HttpHandler =
    choose [
        POST >=> route "/add" >=> add
        POST >=> route "/remove" >=> remove
        POST >=> route "/getSense" >=> getSense
    ]
