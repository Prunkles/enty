namespace enty.Storage.FileSystem

open System.IO
open System.IO.Pipelines
open FSharp.Control
open enty.Core


type IStorage =
    abstract Write: reader: PipeReader * entityId: EntityId -> Async<unit>
    abstract Read: writer: PipeWriter * entityId: EntityId -> Async<unit>
    abstract Delete: entityId: EntityId -> Async<unit>

type IFileSystemStorage =
    inherit IStorage
    abstract GetFiles: entityIds: AsyncSeq<EntityId> -> AsyncSeq<string>


type FileSystemStorage(path: string, ?nestingLevel) =
    let nestingLevel =
        nestingLevel
        |> Option.map (fun n -> if n < 0 || n > 16 then invalidArg (nameof nestingLevel) "nestingLevel < 0 or >= 16" else n)
        |> Option.defaultValue 1
    
    let getEntityFilePath (EntityId entityId) =
        let optimizedPath =
            let bs = entityId.ToByteArray()
            [ for i in 0 .. nestingLevel-1 -> bs.[i].ToString("x2") ]
        let paths = [| path; yield! optimizedPath; string entityId |]
        Path.Combine(paths)
    
    interface IFileSystemStorage with
        
        member this.Write(reader, entityId) = async {
            let entityFilePath = getEntityFilePath entityId
            do Directory.CreateDirectory(Path.GetDirectoryName(entityFilePath)) |> ignore
            use fileStream = File.OpenWrite(entityFilePath)
            let fileWriter = PipeWriter.Create(fileStream)
            do! reader.CopyToAsync(fileWriter) |> Async.AwaitTask
        }
        
        member this.Read(writer, entityId) = async {
            let entityFilePath = getEntityFilePath entityId
            use fileStream = File.OpenRead(entityFilePath)
            let fileReader = PipeReader.Create(fileStream)
            do! fileReader.CopyToAsync(writer) |> Async.AwaitTask
        }

        member this.Delete(entityId) = async {
            let entityFilePath = getEntityFilePath entityId
            // TODO: Cleanup the containing folder if it became empty
            do File.Delete(entityFilePath)
        }

        member this.GetFiles(entityIds) = asyncSeq {
            for entityId in entityIds do
                let path = getEntityFilePath entityId
                yield path
        }

