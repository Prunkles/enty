module enty.ResourceStorage.Server.HttpHandlers

open System
open System.IO
open System.Net.Mime
open System.Security.Cryptography
open System.Text
open Microsoft.AspNetCore.Http
open Microsoft.Net.Http.Headers
open FSharp.Control.Tasks
open Giraffe
open Giraffe.EndpointRouting

open enty.ResourceStorage


let inline getStorage (ctx: HttpContext) = ctx.GetService<IResourceStorage>()

let (|Apply|) f x = f x

let resourceNotFound (ResourceId rid) =
    RequestErrors.notFound (text $"Resource {(rid: Guid)} not found")

let handleRead (Apply ResourceId rid) : HttpHandler = fun next ctx -> task {
    let storage = getStorage ctx
    match! storage.Read(rid) with
    | Ok (resReader, meta) ->
        let headers = ctx.Response.GetTypedHeaders()
        match meta.ContentType with Some x -> headers.ContentType <- MediaTypeHeaderValue(x) | _ -> ()
        match meta.ETag with Some x -> headers.ETag <- EntityTagHeaderValue(x) | _ -> ()
        match meta.LastModified with Some x -> headers.LastModified <- x | _ -> ()
        match meta.ContentLength with Some x -> headers.ContentLength <- x | _ -> ()
        match meta.ContentDisposition with Some x -> headers.ContentDisposition <- ContentDispositionHeaderValue.Parse(x) | _ -> ()

        do! resReader.CopyToAsync(ctx.Response.BodyWriter)
        do! resReader.CompleteAsync()
        return! Successful.ok id next ctx
    | Error () ->
        return! resourceNotFound rid next ctx
}

let handleWrite (Apply ResourceId rid) : HttpHandler = fun next ctx -> task {
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
        let eTag = $"\"%s{hashS}\""
        let contentDisposition =
            file.ContentDisposition
            |> Option.ofObj
            |> Option.map ^fun contentDispositionString ->
                let contentDisposition = ContentDisposition(contentDispositionString)
                contentDisposition.Inline <- true
                contentDisposition.ToString()
        { ContentType = file.ContentType |> Option.ofObj
          ETag = eTag |> Some
          LastModified = DateTimeOffset.Now |> Some
          ContentLength = file.Length |> Some
          ContentDisposition = contentDisposition }

    let! resWriter = storage.Write(rid, resMeta)
    use resWriterStream = resWriter.AsStream()
    resStream.Position <- 0L
    do! resStream.CopyToAsync(resWriterStream)
    do! resWriter.CompleteAsync()
    return! Successful.ok id next ctx
}

let handleDelete (Apply ResourceId rid) : HttpHandler = fun next ctx -> task {
    let storage = getStorage ctx
    do! storage.Delete(rid)
    return! next ctx
}


let endpoints = [
    GET_HEAD [ routef "/%O" handleRead ]
    POST [ routef "/%O" handleWrite ]
    DELETE [ routef "/%O" handleDelete ]
]

let notFoundHandler: HttpHandler =
    "Not Found" |> text |> RequestErrors.notFound
