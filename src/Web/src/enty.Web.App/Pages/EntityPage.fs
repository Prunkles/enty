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
let ForgetEntityButton (onConfirmed: unit -> unit) =
    let isModalOpen, setIsModalOpen = React.useState(false)
    let handleOpenModal () = setIsModalOpen true
    let handleCloseModal () = setIsModalOpen false
    Html.div [
        Mui.modal [
            modal.open' isModalOpen
            modal.onClose (fun _ -> handleCloseModal ())
            modal.children (
                MuiE.boxSx {|
                     position = "absolute"
                     top = "50%"
                     left = "50%"
                     transform = "translate(-50%, -50%)"
                     width = 400
                     bgcolor = "background.paper"
                     border = "2px solid #000"
                     p = 2
                |} @+ [ ] <| [
                    Mui.stack @+ [ stack.spacing 2 ] <| [
                        Mui.typography [
                            typography.align.center
                            typography.children "Are you sure?"
                        ]
                        Mui.button [
                            prop.onClick (fun _ -> onConfirmed ())
                            prop.custom ("color", "error")
                            button.children "Forget"
                            button.variant.contained
                        ]
                    ]
                ]
            )
        ]
        Mui.button [
            prop.text "Forget"
            prop.custom ("color", "error")
            prop.onClick (fun _ -> handleOpenModal ())
        ]
    ]

[<ReactComponent>]
let EntityPage (entityId: EntityId) =
    let entity = React.useAsync(MindApiImpl.mindApi.GetEntities([|entityId|]))

    let handleForgetClicked () =
        Async.startSafe ^ async {
            do! MindApiImpl.mindApi.Forget(entityId)
            Router.navigateBack()
        }

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

            Mui.stack @+ [
                stack.direction.row
                stack.spacing 2
                prop.style [
                    style.justifyContent.center
                ]
            ] <| [
                Mui.button [
                    prop.text "Edit"
                    prop.onClick (fun _ -> Page.EditEntity entityId |> Page.formatPath |> Router.navigatePath)
                ]
                ForgetEntityButton handleForgetClicked
            ]
        ]
    | _ ->
        Mui.circularProgress []
