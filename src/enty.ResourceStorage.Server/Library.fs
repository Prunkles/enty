namespace enty.ResourceStorage.FileSystem

open System
open System.IO
open System.IO.Pipelines
open FSharp.Control
open enty.ResourceStorage


    

type FileSystemResourceStorage(path: string, ?nestingLevel) =
    let nestingLevel = nestingLevel |> Option.defaultValue 1
    do if nestingLevel < 0 || nestingLevel > 16 then
        invalidArg (nameof nestingLevel) "nestingLevel < 0 or >= 16"
    
    let getResourcePath (ridG: Guid) =
        let optimizedPath =
            let bs = ridG.ToByteArray()
            [ for i in 0 .. nestingLevel-1 -> bs.[i].ToString("x2") ]
        let paths = [| path; yield! optimizedPath; string ridG |]
        Path.Combine(paths)
    
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
    
    interface IResourceStorage with
        
        member this.Write(ResourceId ridG, meta) = async {
            let resourcePath = getResourcePath ridG
            // Ensure containing directory exists
            do Directory.CreateDirectory(Path.GetDirectoryName(resourcePath)) |> ignore
            do! writeResourceMeta (resourcePath + ".meta") meta
            use fileStream = File.OpenWrite(resourcePath)
            let fileWriter = PipeWriter.Create(fileStream)
            return fileWriter
        }
        
        member this.Read(ResourceId ridG) = async {
            let resourcePath = getResourcePath ridG
            let! resMeta = readResourceMeta (resourcePath + ".meta")
            use fileStream = File.OpenRead(resourcePath)
            let fileReader = PipeReader.Create(fileStream)
            return fileReader, resMeta
        }

        member this.Delete(ResourceId ridG) = async {
            let resourcePath = getResourcePath ridG
            // TODO: Cleanup the containing folder if it became empty
            do File.Delete(resourcePath)
            do File.Delete(resourcePath + ".meta")
        }
