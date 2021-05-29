namespace enty.ResourceStorage.Server

open System
open System.IO.Pipelines

type ResourceId = ResourceId of Guid
type Resource =
    { Id: ResourceId
      ContentType: string }

type IResourceStorageService =
    abstract Write: rid: ResourceId -> Async<PipeWriter>
    abstract Read: rid: ResourceId -> Async<PipeReader>
    abstract Delete: rid: ResourceId -> Async<unit>
