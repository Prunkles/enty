module enty.Web.App.EntityThumbnail

open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open enty.Utils
open enty.Core

[<RequireQualifiedAccess>]
module Map =

    let (|Item|_|) (key: 'k) (map: Map<'k, 'v>) : 'v option = Map.tryFind key map

let (|Apply|) f x = f x

let parseImage (sense: Sense) =
    option {
        let! image = sense |> Sense.tryItem "image"
        let! resource = image |> Sense.tryItem "resource"
        let! uri = resource |> Sense.tryItem "uri"
        return! uri |> Sense.tryAsValue
    }

let parseTags (sense: Sense) =
    option {
        let! tags = sense |> Sense.tryItem "tags"
        return! tags |> Sense.tryAsList
    }

[<ReactComponent>]
let EntityThumbnail (entity: Entity) (onClicked: unit -> unit) =
    Mui.paper [
        prop.sx {|
            p = 3
            height = "100%"
        |}
        paper.children [
            match entity.Sense with
            | Apply parseImage (Some uriString) ->
                Mui.stack [
                    prop.sx {| height = "100%" |}
                    stack.children [
                        Mui.typography [
                            prop.sx {| overflowWrap = "anywhere" |}
                            typography.children $"{entity.Id}"
                        ]
                        Mui.box [
                            box.sx {|
                                backgroundImage = $"url(\"{uriString}\")"
                                backgroundSize = "contain"
                                backgroundRepeat = "no-repeat"
                                backgroundPosition = "center"
                                height = "100%"
                            |}
                            prop.src uriString
                            prop.onClick (fun _ -> onClicked ())
                        ]
                    ]
                ]
            | _ ->
                Html.text "Undefined entity type"
        ]
    ]
