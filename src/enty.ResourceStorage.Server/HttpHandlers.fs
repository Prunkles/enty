module enty.ResourceStorage.Server.HttpHandlers

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Giraffe
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers
open enty.ResourceStorage


let inline getStorage (ctx: HttpContext) = ctx.GetService<IResourceStorage>()

let (|Apply|) f x = f x

let readHandler (Apply ResourceId rid) : HttpHandler = fun next ctx -> task {
    let storage = getStorage ctx
    match! storage.Read(rid) with
    | Ok (resReader, resMeta) ->
        let headers = ctx.Response.GetTypedHeaders()
        headers.ContentType <- MediaTypeHeaderValue(!> resMeta.ContentType)
        headers.ETag <- EntityTagHeaderValue(!> resMeta.ETag)
        headers.LastModified <- resMeta.LastModified
        do! resReader.CopyToAsync(ctx.Response.BodyWriter)
        do! resReader.CompleteAsync()
        return! Successful.ok id next ctx
    | Error () ->
        return! RequestErrors.notFound (text $"Resource {ResourceId.Unwrap rid} not found") next ctx
}

let writeHandler (Apply ResourceId rid) : HttpHandler = fun next ctx -> task {
    use hashAlg: HashAlgorithm = upcast MD5.Create()
    let storage = getStorage ctx

    let file = ctx.Request.Form.Files.[0]
    use resStream = new MemoryStream()
    do! file.CopyToAsync(resStream)

    resStream.Position <- 0L
    let! hashBs = hashAlg.ComputeHashAsync(resStream)
    let hashS =
        let sb = StringBuilder()
        for b in hashBs do
            sb.Append(b.ToString("x2")) |> ignore
        sb.ToString()

    let resMeta =
        let contentType = file.ContentType
        let eTag = $"\"%s{hashS}\""
        let lastModified = DateTimeOffset.Now
        { ContentType = contentType
          ETag = eTag
          LastModified = lastModified }

    let! resWriter = storage.Write(rid, resMeta)
    use resWriterStream = resWriter.AsStream()
    resStream.Position <- 0L
    do! resStream.CopyToAsync(resWriterStream)
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
