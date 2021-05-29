namespace enty.ResourceStorage.FileSystem

open System
open System.IO
open System.IO.Pipelines
open FSharp.Control


type IDataStorage =
    abstract Write: dataId: Guid -> Async<PipeWriter>
    abstract Read: dataId: Guid -> Async<PipeReader>
    abstract Delete: dataId: Guid -> Async<unit>


type FileSystemDataStorage(path: string, ?nestingLevel) =
    let nestingLevel = nestingLevel |> Option.defaultValue 1
    do if nestingLevel < 0 || nestingLevel > 16 then
        invalidArg (nameof nestingLevel) "nestingLevel < 0 or >= 16"
    
    let getEntityFilePath (dataId: Guid) =
        let optimizedPath =
            let bs = dataId.ToByteArray()
            [ for i in 0 .. nestingLevel-1 -> bs.[i].ToString("x2") ]
        let paths = [| path; yield! optimizedPath; string dataId |]
        Path.Combine(paths)
    
    interface IDataStorage with
        
        member this.Write(dataId) = async {
            let entityFilePath = getEntityFilePath dataId
            do Directory.CreateDirectory(Path.GetDirectoryName(entityFilePath)) |> ignore
            use fileStream = File.OpenWrite(entityFilePath)
            let fileWriter = PipeWriter.Create(fileStream)
            return fileWriter
        }
        
        member this.Read(dataId) = async {
            let entityFilePath = getEntityFilePath dataId
            use fileStream = File.OpenRead(entityFilePath)
            let fileReader = PipeReader.Create(fileStream)
            return fileReader
        }

        member this.Delete(dataId) = async {
            let entityFilePath = getEntityFilePath dataId
            // TODO: Cleanup the containing folder if it became empty
            do File.Delete(entityFilePath)
        }
