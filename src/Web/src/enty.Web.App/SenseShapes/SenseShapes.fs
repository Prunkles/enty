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
            let! image = sense |> Sense.asValue |> SenseValue.tryItem "image"
            let! resource = image |> SenseValue.tryItem "resource"
            let! uri = resource |> SenseValue.tryItem "uri"
            let! uri = uri |> SenseValue.tryAsValue
            // let! uri = Uri.TryCreate(uri, UriKind.RelativeOrAbsolute) |> Option.ofTryByref
            return { Uri = uri }
        }

type TagsSenseShape =
    { Tags: SenseValue list }

[<RequireQualifiedAccess>]
module TagsSenseShape =
    let parse (sense: Sense) : TagsSenseShape option =
        option {
            let! tags = sense |> Sense.asValue |> SenseValue.tryItem "tags"
            let! tags = tags |> SenseValue.tryAsList
            return { Tags = tags }
        }

type UserSenseShape =
    { UserName: string
      Rating: float option }

[<RequireQualifiedAccess>]
module UserSenseShape =
    let parse (sense: Sense) : UserSenseShape option =
        option {
            let! user = sense |> Sense.asValue |> SenseValue.tryItem "user"
            let! username = user |> SenseValue.tryItem "username" |> Option.bind SenseValue.tryAsValue
            let rating = user |> SenseValue.tryItem "rating" |> Option.bind SenseValue.tryAsValue |> Option.bind (Double.TryParse >> Option.ofTryByref)
            return { UserName = username; Rating = rating }
        }

type FeatsSenseShape =
    { Feats: Map<string, SenseValue> }

[<RequireQualifiedAccess>]
module FeatsSenseShape =
    let parse (sense: Sense) : FeatsSenseShape option = option {
        let! feats = sense |> Sense.asValue |> SenseValue.tryItem "feats"
        match feats with
        | SenseValue.Map (SenseMap feats) -> return { Feats = feats }
        | _ -> return! None
    }
