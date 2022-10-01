namespace enty.Web.App.SenseShapes

open System
open FsToolkit.ErrorHandling
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

type UserSenseShape =
    { UserName: string
      Rating: float option }

[<RequireQualifiedAccess>]
module UserSenseShape =
    let parse (sense: Sense) : UserSenseShape option =
        option {
            let! user = sense |> Sense.tryItem "user"
            let! username = user |> Sense.tryItem "username" |> Option.bind Sense.tryAsValue
            let rating = user |> Sense.tryItem "rating" |> Option.bind Sense.tryAsValue |> Option.bind (Double.TryParse >> Option.ofTryByref)
            return { UserName = username; Rating = rating }
        }

type FeatsSenseShape =
    { Feats: Map<string, Sense> }

[<RequireQualifiedAccess>]
module FeatsSenseShape =
    let parse (sense: Sense) : FeatsSenseShape option = option {
        let! feats = sense |> Sense.tryItem "feats"
        match feats with
        | Sense.Map feats -> return { Feats = feats }
        | _ -> return! None
    }
