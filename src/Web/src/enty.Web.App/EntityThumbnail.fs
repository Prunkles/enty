module enty.Web.App.EntityThumbnail

open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open enty.Utils
open enty.Core
open enty.Web.App.Utils
open enty.Web.App.SenseShapes

[<RequireQualifiedAccess>]
module Map =

    let (|Item|_|) (key: 'k) (map: Map<'k, 'v>) : 'v option = Map.tryFind key map

let (|Apply|) f x = f x

[<ReactComponent>]
let EntityThumbnail (entity: Entity) (onClicked: unit -> unit) =
    Mui.paper @+ [
        prop.sx {|
            p = 3
            height = "100%"
        |}
        prop.onClick (fun _ -> onClicked ())
    ] <| [
        match entity.Sense with
        | Apply ImageSenseShape.parse (Some imageSense) ->
            Mui.stack @+ [
                prop.sx {| height = "100%" |}
            ] <| [
                Mui.typography [
                    prop.sx {| overflowWrap = "anywhere" |}
                    typography.children $"{entity.Id}"
                ]
                Mui.box [
                    box.sx {|
                        backgroundImage = $"url(\"%s{imageSense.Uri}\")"
                        backgroundSize = "contain"
                        backgroundRepeat = "no-repeat"
                        backgroundPosition = "center"
                        height = "100%"
                    |}
                    prop.src imageSense.Uri
                ]
            ]
        | _ ->
            Html.div @+ [] <| [
                Html.h1 (string entity.Id)
                Html.text "Undefined entity type"
            ]
    ]
