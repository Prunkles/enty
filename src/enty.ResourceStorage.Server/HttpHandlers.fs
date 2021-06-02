module enty.ResourceStorage.Server.HttpHandlers

open System
open System.IO
open System.IO.Pipelines
open System.Net.Mime
open Giraffe
open FSharp.Control.Tasks.V2
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

let readHandler (ridG: Guid) : HttpHandler = fun next ctx -> task {
    let rid = ResourceId ridG
    let storage = getStorage ctx
    let writer = ctx.Response.BodyWriter
    let! resReader, resMeta = storage.Read(rid)
    do ctx.SetHttpHeader "Content-Type" resMeta.ContentType
    do! resReader.CopyToAsync(ctx.Response.BodyWriter)
    return! Successful.OK () next ctx
}

let (|Apply|) f x = f x

let writeHandler (Apply ResourceId rid) : HttpHandler = fun next ctx -> task {
    let storage = getStorage ctx
    let file = ctx.Request.Form.Files.[0]
    let resMeta =
        let contentType = file.ContentType
        { ContentType = contentType }
    let! resWriter = storage.Write(rid, resMeta)
    do! file.CopyToAsync(resWriter.AsStream())
    return! Successful.OK () next ctx
}

let deleteHandler (Apply ResourceId rid) : HttpHandler = fun next ctx -> task {
    let storage = getStorage ctx
    do! storage.Delete(rid)
    return! next ctx
}

let server: HttpHandler =
    choose [
        GET >=> routef "/%O" readHandler
        POST >=> routef "/%O" writeHandler
        DELETE >=> routef "/%O" deleteHandler
        RequestErrors.NOT_FOUND "Not Found"
    ]
