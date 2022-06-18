namespace enty.Web.App.SenseShapes

open System
open enty.Core
open enty.Utils

type ImageSenseShape =
    { Uri: string }

[<RequireQualifiedAccess>]
module ImageSenseShape =
    let parse (sense: Sense) : ImageSenseShape option =
        option {
            let! image = sense |> Sense.tryItem "image"
            let! resource = image |> Sense.tryItem "resource"
            let! uri = resource |> Sense.tryItem "uri"
            let! uri = uri |> Sense.tryAsValue
            // let! uri = Uri.TryCreate(uri, UriKind.RelativeOrAbsolute) |> Option.ofTryByref
            return { Uri = uri }
        }

type TagsSenseShape =
    { Tags: Sense list }

[<RequireQualifiedAccess>]
module TagsSenseShape =
    let parse (sense: Sense) : TagsSenseShape option =
        option {
            let! tags = sense |> Sense.tryItem "tags"
            let! tags = tags |> Sense.tryAsList
            return { Tags = tags }
        }
