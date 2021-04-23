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
    let entityId = EntityId entityId
    use output = Console.OpenStandardOutput()
    let writer = PipeWriter.Create(output)
    do! storage.Read(writer, entityId)
}

let writeToConsole (storage: IStorage) entityId = async {
    let entityId = EntityId entityId
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
            return Option.ofObj entityIdStr |> Option.map (fun e -> e, ())
        }) ()
        |> AsyncSeq.map (Guid.Parse >> EntityId)
    do! printFiles storage entityIds
}

[<EntryPoint>]
let main argv =
    let storagePath = "/tmp/enty/storage"
    Directory.CreateDirectory(storagePath) |> ignore
    let storage: IFileSystemStorage = upcast FileSystemStorage(storagePath)

    let checkStructure =
        #if DEBUG
            true
        #else
            false
        #endif
    let parser = ArgumentParser.Create<StorageArgs>(programName="enty-storage", checkStructure=checkStructure)
    
    async {
        try
            let results = parser.ParseCommandLine(argv, raiseOnUsage=true)
            match results.GetAllResults() with
            | [ StorageArgs.Read results ] ->
                let entityId = results.GetResult(ReadArgs.EntityId)
                do! readFromConsole storage entityId
            | [ StorageArgs.Write results ] ->
                let entityId = results.GetResult(WriteArgs.EntityId)
                do! writeToConsole storage entityId
            | [ StorageArgs.Delete results ] ->
                let entityId = results.GetResult(DeleteArgs.EntityId) |> EntityId
                do! storage.Delete(entityId)
            | [ StorageArgs.Files results ] ->
                match results.GetAllResults() with
                | [ FilesArgs.Stream ] -> do! printFilesFromConsole storage
                | [ FilesArgs.EntityIds entityIds ] ->
                    let entityIds = entityIds |> Seq.map EntityId |> AsyncSeq.ofSeq
                    do! printFiles storage entityIds
                | _ -> invalidOp ""
            | _ -> invalidOp ""
        with
        | :? ArguException as e ->
            printfn $"{e.Message}"
    } |> Async.RunSynchronously
    
    0
