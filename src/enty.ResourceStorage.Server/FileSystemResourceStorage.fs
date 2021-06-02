namespace enty.ResourceStorage.FileSystem

open System
open System.IO
open System.IO.Pipelines
open FSharp.Control
open Microsoft.Extensions.Logging
open enty.ResourceStorage


type FileSystemResourceStorage(logger: ILogger<FileSystemResourceStorage>, path: string, ?nestingLevel) =
    let nestingLevel = nestingLevel |> Option.defaultValue 1
    do if nestingLevel < 0 || nestingLevel > 16 then
        invalidArg (nameof nestingLevel) "nestingLevel < 0 or >= 16"

    let getResourcePath (ridG: Guid) =
        let optimizedPath =
            let bs = ridG.ToByteArray()
            [ for i in 0 .. nestingLevel-1 -> bs.[i].ToString("x2") ]
        let segments = [| path; yield! optimizedPath; string ridG |]
        Path.Combine(segments)

    let writeResourceMeta (path: string) (resMeta: ResourceMeta) = async {
        let lines =
            ResourceMeta.toMap resMeta
            |> Seq.map (fun (KeyValue (k, v)) -> sprintf "%s:%s" k v)
        do! File.WriteAllLinesAsync(path, lines) |> Async.AwaitTask
    }

    let readResourceMeta (path: string) = async {
        let! lines = File.ReadAllLinesAsync(path) |> Async.AwaitTask
        let resMeta =
            lines
            |> Seq.map ^fun s -> let x = s.Split(':') in x.[0], x.[1]
            |> Map.ofSeq
            |> ResourceMeta.ofMap
        match resMeta with
        | Some resMeta -> return resMeta
        | None -> return failwith "ResourceMeta"
    }

    let (|AggregateExceptionInner|_|) (ex: exn) =
        match ex with :? AggregateException as aggrEx -> Some aggrEx.InnerException | _ -> None

    interface IResourceStorage with

        member this.Write(ResourceId ridG, meta) = async {
            logger.LogInformation($"Writing resource {ridG}")
            let resourcePath = getResourcePath ridG
            // Ensure containing directory exists
            do Directory.CreateDirectory(Path.GetDirectoryName(resourcePath)) |> ignore
            do! writeResourceMeta (resourcePath + ".meta") meta
            let fileStream = File.OpenWrite(resourcePath)
            let fileWriter = PipeWriter.Create(fileStream, StreamPipeWriterOptions(leaveOpen=false))
            return fileWriter
        }

        member this.Read(ResourceId ridG) = async {
            logger.LogInformation($"Reading resource {ridG}")
            let resourcePath = getResourcePath ridG
            try
                let! resMeta = readResourceMeta (resourcePath + ".meta")
                let fileStream = File.OpenRead(resourcePath)
                let fileReader = PipeReader.Create(fileStream, StreamPipeReaderOptions(leaveOpen=false))
                return Ok (fileReader, resMeta)
            with
            | AggregateExceptionInner (:? FileNotFoundException | :? DirectoryNotFoundException) ->
                return Error ()
        }

        member this.Delete(ResourceId ridG) = async {
            logger.LogInformation($"Deleting resource {ridG}")
            let resourcePath = getResourcePath ridG
            // TODO: Cleanup the containing folder if it became empty
            do File.Delete(resourcePath)
            do File.Delete(resourcePath + ".meta")
        }
