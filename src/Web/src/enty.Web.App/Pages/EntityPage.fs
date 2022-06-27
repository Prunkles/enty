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
        Html.div [
            yield Html.text (string entity.Id)
            match entity.Sense |> TagsSenseShape.parse with
            | Some tagsSense ->
                yield Mui.stack [
                    stack.direction.row
                    stack.spacing 0.5
                    stack.children [
                        for tag in tagsSense.Tags do
                            Mui.chip [
                                chip.label (Sense.format tag)
                                chip.variant.outlined
                            ]
                    ]
                ]
            | _ -> ()
            match entity.Sense |> ImageSenseShape.parse with
            | Some imageSense ->
                yield Html.div [
                    Html.img [
                        prop.src imageSense.Uri
                    ]
                ]
            | _ -> ()
            yield Html.pre (entity.Sense |> Sense.formatMultiline)
            yield Mui.button [
                prop.text "Edit"
                prop.onClick (fun _ -> Page.EditEntity entityId |> Page.formatPath |> Router.navigatePath)
            ]
        ]
    | _ ->
        Mui.circularProgress []
