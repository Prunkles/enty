module enty.ResourceStorage.Server.HttpHandlers

open System
open System.IO
open System.IO.Pipelines
open System.Net.Mime
open Giraffe
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open enty.ResourceStorage.FileSystem

type Resource =
    { Id: Guid
      Extension: string }

module Resource =
    
    let create id (ext: string) =
        let ext = if ext.Length > 0 && ext.[0] = '.' then ext.Remove(0, 1) else ext
        { Id = id; Extension = ext }
    
    let format (resource: Resource) = sprintf "%O.%s" resource.Id resource.Extension

module Mime =
    open MimeTypes
    let ofExt ext = MimeTypeMap.GetMimeType(ext)
    let toExt mime = MimeTypeMap.GetExtension(mime)

/// aabbcc.foo  ==>  aa/bb/cc/aabbcc.foo
let getResourcePath (res: Resource) =
    /// aa/bb/cc
    let getOptimizedDir nestingLevel (rid: Guid) =
        let optimizedPath =
            let bs = rid.ToByteArray()
            [| for i in 0 .. nestingLevel-1 -> bs.[i].ToString("x2") |]
        Path.Combine(optimizedPath)
    let optimizedDirPath = getOptimizedDir 2 res.Id
    Path.Combine(optimizedDirPath, Resource.format res)

let getStoragePath (ctx: HttpContext) = ctx.GetService<IConfiguration>().["Storage:Path"]


let readHandler (fullRid: string) : HttpHandler = fun next ctx -> task {
    let storagePath = getStoragePath ctx
    let writer = ctx.Response.BodyWriter
    try
        let resourcePath = Path.Combine(storagePath, getResourcePath fullRid)
        let ext = Path.GetExtension(resourcePath)
        let mime = Mime.ofExt ext
        ctx.SetContentType(mime)
        let writer = ctx.Response.BodyWriter
        let reader = PipeReader.Create(File.OpenRead(resourcePath))
        do! reader.CopyToAsync(writer)
        do! reader.CompleteAsync()
        return! earlyReturn ctx
    with _ ->
        return! RequestErrors.BAD_REQUEST "EntityId not found" next ctx
}

let writeHandler (filename: string) : HttpHandler = fun next ctx -> task {
    ctx.Request.Form.Files.[0].
    return! next ctx
}

let deleteHandler eid : HttpHandler = fun next ctx -> task {
    let storage = ctx.GetService<IDataStorage>()
    do! storage.Delete(eid)
    return! next ctx
}

let server: HttpHandler =
    choose [
        GET >=> routef "/%s" readHandler
        POST >=> routef "/%s" writeHandler
        DELETE >=> routef "/%s" deleteHandler
        RequestErrors.NOT_FOUND "Not Found"
    ]
