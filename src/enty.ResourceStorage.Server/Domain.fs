[<AutoOpen>]
module enty.ResourceStorage.Domain

open System
open enty.Utils

type ResourceId = ResourceId of Guid
    with static member Unwrap(ResourceId x) = x

type ResourceMeta =
    { ContentType: string option
      ETag: string option
      LastModified: DateTimeOffset option
      ContentLength: int64 option
      ContentDisposition: string option }

type Resource =
    { Id: ResourceId
      Meta: ResourceMeta }

[<RequireQualifiedAccess>]
module ResourceMeta =

    let toHeaders (meta: ResourceMeta) : Map<string, string> =
        [ "Content-Type", meta.ContentType
          "ETag", meta.ETag
          "Last-Modified", meta.LastModified |> Option.map (fun x -> x.ToString())
          "Content-Length", meta.ContentLength |> Option.map string
          "Content-Disposition", meta.ContentDisposition ]
        |> Seq.collect ^fun (name, value) ->
            match value with
            | Some value -> [name, value]
            | None -> []
        |> Map.ofSeq

    let ofHeaders (headers: Map<string, string>) : ResourceMeta =
        { ContentType = headers |> Map.tryFind "Content-Type"
          ETag = headers |> Map.tryFind "ETag"
          LastModified = headers |> Map.tryFind "Last-Modified" |> Option.bind (DateTimeOffset.TryParse >> Option.ofTryByref)
          ContentLength = headers |> Map.tryFind "Content-Length" |> Option.bind (Int64.TryParse >> Option.ofTryByref)
          ContentDisposition = headers |> Map.tryFind "Content-Disposition" }
