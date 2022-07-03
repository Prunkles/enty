[<AutoOpen>]
module enty.Web.App.Pages.EntityPage

open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5
open Feliz.Router
open enty.Core
open enty.Web.App
open enty.Web.App.SenseFormatting
open enty.Web.App.SenseShapes
open enty.Web.App.Utils

[<ReactComponent>]
let EntityPage (entityId: EntityId) =
    let entity = React.useAsync(MindApiImpl.mindApi.GetEntities([|entityId|]))
    match entity with
    | Some [| entity |] ->
        Mui.stack @+ [ stack.spacing 4 ] <| [

            MuiE.boxSx {| alignSelf = "flex-start" |} @+ [] <| [
                EntityThumbnail.EntityIdHint entity.Id
            ]

            match entity.Sense |> TagsSenseShape.parse with
            | Some tagsSense ->
                Mui.stack @+ [
                    stack.direction.row
                    stack.spacing 0.5
                ] <| [
                    for tag in tagsSense.Tags do
                        Mui.chip [
                            chip.label (Sense.format tag)
                            chip.variant.outlined
                        ]
                ]
            | _ -> ()

            match entity.Sense |> ImageSenseShape.parse with
            | Some imageSense ->
                Html.a @+ [
                    prop.href imageSense.Uri
                    prop.style [ style.width.minContent ]
                ] <| [
                    Html.img [
                        prop.style [
                            style.maxHeight (length.vh 100)
                        ]
                        prop.src imageSense.Uri
                    ]
                ]
            | _ -> ()

            Mui.paper @+ [
            ] <| [
                Html.pre [
                    // prop.sx {| m = 0 |}
                    prop.style [
                        style.overflowX.scroll
                        style.margin (length.px 0)
                    ]
                    prop.text (entity.Sense |> Sense.formatMultiline)
                ]
            ]

            Mui.button [
                prop.text "Edit"
                prop.onClick (fun _ -> Page.EditEntity entityId |> Page.formatPath |> Router.navigatePath)
            ]
        ]
    | _ ->
        Mui.circularProgress []
