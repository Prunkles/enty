module enty.Storage.FileSystem.Cli.Program

open System
open System.IO
open System.IO.Pipelines
open Argu
open FSharp.Control
open enty.Core
open enty.Storage.FileSystem
open enty.Storage.FileSystem.Cli.Args

let readFromConsole (storage: IStorage) entityId = async {
    use output = Console.OpenStandardOutput()
    let writer = PipeWriter.Create(output)
    do! storage.Read(writer, entityId)
}

let writeToConsole (storage: IStorage) entityId = async {
    use input = Console.OpenStandardInput()
    let reader = PipeReader.Create(input)
    do! storage.Write(reader, entityId)
}

let printFiles (storage: IFileSystemStorage) (entityIds: AsyncSeq<EntityId>) =
    storage.GetFiles(entityIds)
    |> AsyncSeq.iterAsync (fun path ->
        Console.Out.WriteLineAsync(path) |> Async.AwaitTask
    )

let printFilesFromConsole (storage: IFileSystemStorage) = async {
    let entityIds =
        AsyncSeq.unfoldAsync (fun () -> async {
            let! entityIdStr = Console.In.ReadLineAsync() |> Async.AwaitTask
            return
                match entityIdStr with
                | null -> None
                | s -> Some (s, ())
        }) ()
        |> AsyncSeq.map (Guid.Parse >> EntityId)
    do! printFiles storage entityIds
}


let (|ParseGuid|_|) (s: string) =
    match Guid.TryParse(s) with
    | true, g -> Some g
    | false, _ -> None

let parseCla (args: string list) : Result<StorageArgs, string> =
    match args with
    | "write" :: args ->
        match args with
        | [ ParseGuid entityId ] -> Ok { WriteArgs.EntityId = entityId }
        | _ -> Error ""
        |> Result.map StorageArgs.Write
    | "read" :: args ->
        match args with
        | [ ParseGuid entityId ] -> Ok { ReadArgs.EntityId = entityId }
        | _ -> Error ""
        |> Result.map StorageArgs.Read
    | "delete" :: args ->
        match args with
        | [ ParseGuid entityId ] -> Ok { DeleteArgs.EntityId = entityId }
        | _ -> Error ""
        |> Result.map StorageArgs.Delete
    | "files" :: args ->
        match args with
        | [ "--stream" ] -> Ok FilesArgs.Stream
        | entityIds ->
            (Some [], entityIds)
            ||> Seq.fold (fun state entityId ->
                match state, entityId with
                | Some ids, ParseGuid entityId -> Some (entityId :: ids)
                | _ -> None
            )
            |> function
            | None -> Error ""
            | Some ids -> Ok (FilesArgs.List ids)
        |> Result.map StorageArgs.Files
    | _ ->
        Error ""



[<EntryPoint>]
let main argv =
    let storagePath = "/tmp/enty/storage"
    Directory.CreateDirectory(storagePath) |> ignore
    let storage: IFileSystemStorage = upcast FileSystemStorage(storagePath)

    let args = parseCla (Array.toList argv)
    
    async {
        match args with
        | Error e -> eprintfn $"{e}"
        | Ok args ->
            match args with
            | StorageArgs.Read args ->
                let entityId = EntityId args.EntityId
                do! readFromConsole storage entityId
            | StorageArgs.Write args ->
                let entityId = EntityId args.EntityId
                do! writeToConsole storage entityId
            | StorageArgs.Delete args ->
                let entityId = EntityId args.EntityId
                do! storage.Delete(entityId)
            | StorageArgs.Files args ->
                match args with
                | FilesArgs.Stream ->
                    do! printFilesFromConsole storage
                | FilesArgs.List entityIds ->
                    let entityIds = entityIds |> Seq.map EntityId
                    do! printFiles storage (AsyncSeq.ofSeq entityIds)
    } |> Async.RunSynchronously
    
    0
