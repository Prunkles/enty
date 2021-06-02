namespace enty.ResourceStorage

open System.IO.Pipelines

type IResourceStorage =
    abstract Write: rid: ResourceId * meta: ResourceMeta -> Async<PipeWriter>
    abstract Read: rid: ResourceId -> Async<Result<PipeReader * ResourceMeta, unit>>
    abstract Delete: rid: ResourceId -> Async<unit>
