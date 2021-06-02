module enty.ResourceStorage.Server.HttpHandlers

open System
open System.IO
open System.IO.Pipelines
open System.Net.Mime
open Giraffe
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open enty.ResourceStorage


//module Mime =
//    open MimeTypes
//    let ofExt ext = MimeTypeMap.GetMimeType(ext)
//    let toExt mime = MimeTypeMap.GetExtension(mime)

///// aabbcc.foo  ==>  aa/bb/cc/aabbcc.foo
//let getResourcePath (res: Resource) =
//    /// aa/bb/cc
//    let getOptimizedDir nestingLevel (rid: Guid) =
//        let optimizedPath =
//            let bs = rid.ToByteArray()
//            [| for i in 0 .. nestingLevel-1 -> bs.[i].ToString("x2") |]
//        Path.Combine(optimizedPath)
//    let optimizedDirPath = getOptimizedDir 2 res.Id
//    Path.Combine(optimizedDirPath, Resource.format res)

//let getStoragePath (ctx: HttpContext) = ctx.GetService<IConfiguration>().["Storage:Path"]

let inline getStorage (ctx: HttpContext) = ctx.GetService<IResourceStorage>()

let (|Apply|) f x = f x

let readHandler (Apply ResourceId rid) : HttpHandler = fun next ctx -> task {
    let storage = getStorage ctx
    match! storage.Read(rid) with
    | Ok (resReader, resMeta) ->
        do ctx.SetHttpHeader("Content-Type", resMeta.ContentType)
        do! resReader.CopyToAsync(ctx.Response.BodyWriter)
        do! resReader.CompleteAsync()
        return! Successful.ok id next ctx
    | Error () ->
        return! RequestErrors.notFound (text $"Resource {rid.Unwrap} not found") next ctx
}

let writeHandler (Apply ResourceId rid) : HttpHandler = fun next ctx -> task {
    let storage = getStorage ctx
    let file = ctx.Request.Form.Files.[0]
    let resMeta =
        let contentType = file.ContentType
        { ContentType = contentType }
    let! resWriter = storage.Write(rid, resMeta)
    use resWriterStream = resWriter.AsStream()
    do! file.CopyToAsync(resWriterStream)
    do! resWriter.CompleteAsync()
    return! Successful.ok id next ctx
}

let deleteHandler (Apply ResourceId rid) : HttpHandler = fun next ctx -> task {
    let storage = getStorage ctx
    do! storage.Delete(rid)
    return! next ctx
}


open Giraffe.EndpointRouting

let endpoints = [
    GET [ routef "/%O" readHandler ]
    POST [ routef "/%O" writeHandler ]
    DELETE [ routef "/%O" deleteHandler ]
]

let notFoundHandler: HttpHandler =
    "Not Found" |> text |> RequestErrors.notFound

//let server: HttpHandler =
//    choose [
//        GET >=> routef "/%O" readHandler
//        POST >=> routef "/%O" writeHandler
//        DELETE >=> routef "/%O" deleteHandler
//        RequestErrors.NOT_FOUND "Not Found"
//    ]
