module enty.Storage.Server.HttpHandlers

open System
open Giraffe
open enty.Core
open FSharp.Control.Tasks.V2
open enty.Storage.FileSystem


let readHandler (eid: EntityId) : HttpHandler =
    fun next ctx -> task {
        let storage = ctx.GetService<IStorage>()
        let writer = ctx.Response.BodyWriter
        do! storage.Read(writer, eid)
        return! next ctx
    }

let writeHandler (eid: EntityId) : HttpHandler = fun next ctx -> task {
    let storage = ctx.GetService<IStorage>()
    let reader = ctx.Request.BodyReader
    do! storage.Write(reader, eid)
    return! next ctx
}

let deleteHandler eid : HttpHandler = fun next ctx -> task {
    let storage = ctx.GetService<IStorage>()
    do! storage.Delete(eid)
    return! next ctx
}

let server: HttpHandler =
    choose [
        GET >=> routef "/read/%O" (fun guid -> readHandler (EntityId guid))
        POST >=> routef "/write/%O" (fun guid -> writeHandler (EntityId guid))
        DELETE >=> routef "/delete/%O" (fun guid -> deleteHandler (EntityId guid))
        RequestErrors.NOT_FOUND "Not Found"
    ]