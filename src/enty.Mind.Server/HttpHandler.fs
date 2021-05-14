module enty.Mind.Server.HttpHandler

open System
open FSharp.Control
open FSharp.Data
open Giraffe
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Http

open Microsoft.Extensions.Logging
open enty.Utils
open enty.Core
open enty.Mind
open enty.Mind.Server.Api
open SenseJson
open enty.Mind.WishParsing


let parseAddRequest (ctx: HttpContext) = task {
    let! requestBody = ctx.ReadBodyFromRequestAsync()
    match JsonValue.Parse(requestBody) with
    | JsonValue.Record [| "EntityId", JsonValue.String entityId; "Sense", sense |] ->
        return
            { EntityId = Guid.Parse(entityId)
              Sense = sense }
    | _ -> return invalidArg "" ""
}

let rememberHandler: HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()

    let! addRequest = parseAddRequest ctx
    
    let entityId = EntityId addRequest.EntityId
    let sense = Sense.ofJson addRequest.Sense
    
    do! mindService.Remember(entityId, sense)
    
    return! next ctx
}

let forgetHandler: HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()
    let! (removeRequest: RemoveRequest) = ctx.BindJsonAsync()
    let entityId = removeRequest.EntityId |> EntityId
    do! mindService.Forget(entityId)
    return! next ctx
}

let getSenseHandler: HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()
    let! (getSenseRequest: GetSenseRequest) = ctx.BindJsonAsync()
    let entityId = getSenseRequest.EntityId |> EntityId
    let! sense = mindService.GetSense(entityId)
    let jsonValue = sense |> Sense.toJson
    let json = jsonValue.ToString()
    
    do ctx.SetContentType("application/json")
    let! _ = ctx.WriteStringAsync(json)
    
    return! next ctx
}

let wishHandler: HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()
    let! (request: WishRequest) = ctx.BindJsonAsync()
    
    let wish = request.WishString |> Wish.parse |> Result.getOk
    let entityIds = mindService.Wish(wish)
    
    let { PaginationRequest.Page = page; PageSize = pageSize } = request.Pagination
    let paged =
        entityIds
        |> AsyncSeq.skip (page * pageSize)
        |> AsyncSeq.take pageSize
    
    let! responseEntityIds = paged |> AsyncSeq.map (fun (EntityId x) -> x) |> AsyncSeq.toArrayAsync
    let pages = 1
    
    let response =
        { EntityIds = responseEntityIds
          Pagination = { Page = page; PageSize = pageSize; Pages = pages } }
    
    return! ctx.WriteJsonAsync(response)
}

let errorHandler (ex: exn) (logger: ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse
    >=> ServerErrors.INTERNAL_ERROR ex.Message

let app: HttpHandler =
    choose [
        POST >=> route "/remember" >=> rememberHandler
        POST >=> route "/forget" >=> forgetHandler
        POST >=> route "/getSense" >=> getSenseHandler
        POST >=> route "/wish" >=> wishHandler
        RequestErrors.NOT_FOUND "Not found"
    ]
