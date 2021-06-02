[<AutoOpen>]
module enty.ResourceStorage.Domain

open System

type ResourceId = ResourceId of Guid
    with member this.Unwrap = let (ResourceId x) = this in x

type ResourceMeta =
    { ContentType: string }

type Resource =
    { Id: ResourceId
      Meta: ResourceMeta }


module ResourceMeta =

    let toMap (resMeta: ResourceMeta) : Map<string, string> =
        [ "Content-Type", resMeta.ContentType ]
        |> Map.ofList

    let ofMap (mp: Map<string, string>) : ResourceMeta option =
        option {
            let! contentType = mp |> Map.tryFind "Content-Type"
            return { ContentType = contentType }
        }
