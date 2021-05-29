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
open enty.Storage.FileSystem

let readHandler (eidGuid: Guid) : HttpHandler = fun next ctx -> task {
    let eid = EntityId eidGuid
    let mime = Sense.Feature.File.Mime.mime entity.Sense
    match mime with
    | None -> return! skipPipeline
    | Some mime ->
        let client = ctx.GetService<IHttpClientFactory>().CreateClient("storage")
        let! resp = client.GetAsync($"/read/{eidGuid}")
        let! respStream = resp.Content.ReadAsStreamAsync()
        ctx.SetContentType(mime)
        do! respStream.CopyToAsync(ctx.Response.Body)
        do! ctx.Response.CompleteAsync()
        return! earlyReturn ctx
}

let getStorageOrigin (cont: string -> HttpHandler) : HttpHandler = fun next ctx -> task {
    let configuration = ctx.GetService<IConfiguration>()
    let path = configuration.["Storage:Url"]
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