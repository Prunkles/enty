module enty.Web.App.Pages.EditEntityPage

open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open Feliz.Router
open enty.Core

open enty.Web.App
open enty.Web.App.Utils
open enty.Web.App.EntityCreating
open enty.Web.App.SenseFormatting
open enty.Web.App.SenseParsing


[<ReactComponent>]
let EditEntityPage (entityId: EntityId) =
    let isCreatedSnackbarOpened, setIsCreatedSnackbarOpened = React.useState(false)
    let handleSenseCreated (sense: Sense) =
        async {
            let senseString = sense |> Sense.format
            let! result = MindApiImpl.mindApi.Remember(entityId, senseString)
            match result with
            | Ok () ->
                setIsCreatedSnackbarOpened true
                Router.navigateBack()
            | Error reason -> eprintfn $"Failed edit entity: {reason}"
        }
        |> Async.startSafe
    let handleCreatedSnackbarClosed () =
        setIsCreatedSnackbarOpened false
    let entity = React.useAsync(async {
        match! MindApiImpl.mindApi.GetEntities([|entityId|]) with
        | [| entity |] -> return entity
        | _ -> return failwith $"Entity {entityId} not found"
    })
    match entity with
    | Some entity ->
        React.fragment [
            EntityCreateForm handleSenseCreated entity.Sense "Edit"
            Mui.snackbar [
                snackbar.open' isCreatedSnackbarOpened
                snackbar.onClose (fun _ -> handleCreatedSnackbarClosed ())
                snackbar.autoHideDuration 6000
                snackbar.message "Edited!"
                prop.onClick (fun _ -> handleCreatedSnackbarClosed ())
            ]
        ]
    | None ->
        Mui.circularProgress []
