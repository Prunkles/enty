namespace enty.ResourceStorage

open System
open System.IO.Pipelines

type ResourceId = ResourceId of Guid

type ResourceMeta =
    { ContentType: string }

type Resource =
    { Id: ResourceId
      Meta: ResourceMeta }

type IResourceStorage =
    abstract Write: rid: ResourceId * meta: ResourceMeta -> Async<PipeWriter>
    abstract Read: rid: ResourceId -> Async<PipeReader * ResourceMeta>
    abstract Delete: rid: ResourceId -> Async<unit>

module ResourceMeta =
    
    let toMap (resMeta: ResourceMeta) : Map<string, string> =
        [ "Content-Type", resMeta.ContentType ]
        |> Map.ofList
    
    let ofMap (mp: Map<string, string>) : ResourceMeta option =
        option {
            let! contentType = mp |> Map.tryFind "Content-Type"
            return { ContentType = contentType }
        }
