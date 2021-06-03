[<AutoOpen>]
module enty.ResourceStorage.Domain

open System
open enty.Utils

type ResourceId = ResourceId of Guid
    with static member Unwrap(ResourceId x) = x

type ResourceMeta =
    { ContentType: string
      ETag: string
      LastModified: DateTimeOffset }

type Resource =
    { Id: ResourceId
      Meta: ResourceMeta }


module ResourceMeta =

    let toMap (resMeta: ResourceMeta) : Map<string, string> =
        [ "Content-Type", resMeta.ContentType
          "ETag", resMeta.ETag
          "Last-Modified", resMeta.LastModified.ToString() ]
        |> Map.ofList

    let ofMap (mp: Map<string, string>) : ResourceMeta option =
        option {
            let! contentType = mp |> Map.tryFind "Content-Type"
            and! eTag = mp |> Map.tryFind "ETag"
            and! lastModified = mp |> Map.tryFind "Last-Modified" |> Option.bind (DateTimeOffset.TryParse >> Option.ofTryByref)
            return { ContentType = contentType
                     ETag = eTag
                     LastModified = lastModified }
        }
