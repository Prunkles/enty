module enty.Storage.Typed.Server.HttpHandlers

open System
open System.IO.Pipelines
open System.Net.Http
open FSharp.Control.Tasks.V2
open Giraffe
open Microsoft.Extensions.Configuration
open enty.Core
open enty.Core.Traits
open enty.Mind
open enty.Mind.Server.Api
open enty.Storage.FileSystem

let readHandler (eidGuid: Guid) : HttpHandler = fun next ctx -> task {
    let mindService = ctx.GetService<IMindService>()
    let eid = EntityId eidGuid
    let! entities = mindService.GetEntities([|eid|])
    let entity = entities.[0]
    let mime = Sense.Feature.File.Mime.mime entity.Sense
    match mime with
    | None -> return! skipPipeline
    | Some mime ->
        let client = ctx.GetService<IHttpClientFactory>().CreateClient()
        let pipe = Pipe()
        let! resp = client.GetAsync($"/read/{eidGuid}")
        do! resp.Content.CopyToAsync(pipe.Writer.AsStream())
        ctx.SetContentType(mime)
        do! pipe.Reader.CopyToAsync(ctx.Response.BodyWriter)
        return! earlyReturn ctx
}

let getStorageOrigin (cont: string -> HttpHandler) : HttpHandler = fun next ctx -> task {
    let configuration = ctx.GetService<IConfiguration>()
    let path = configuration.["Storage:Origin"]
    return! cont path next ctx
}

//let redirectToOriginHandler: HttpHandler = fun next ctx -> task {
//    ()
//}

let server: HttpHandler =
    choose [
        GET >=> routef "/read/%O" readHandler
        getStorageOrigin (redirectTo true)
    ]