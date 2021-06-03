[<AutoOpen>]
module enty.ResourceStorage.Domain

open System
open enty.Utils

type ResourceId = ResourceId of Guid
    with member this.Unwrap = let (ResourceId x) = this in x

type ResourceMeta =
    { ContentType: string
      ETag: string }

type Resource =
    { Id: ResourceId
      Meta: ResourceMeta }


module ResourceMeta =

    let toMap (resMeta: ResourceMeta) : Map<string, string> =
        [ "Content-Type", resMeta.ContentType
          "ETag", resMeta.ETag ]
        |> Map.ofList

    let ofMap (mp: Map<string, string>) : ResourceMeta option =
        option {
            let! contentType = mp |> Map.tryFind "Content-Type"
            and! eTag = mp |> Map.tryFind "ETag"
            return { ContentType = contentType
                     ETag = eTag }
        }
