[<AutoOpen>]
module enty.Web.App.Pages.CreateEntityPage

open System
open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open enty.Core

open enty.Web.App
open enty.Web.App.Utils
open enty.Web.App.EntityCreating
open enty.Web.App.SenseFormatting
open enty.Web.App.SenseParsing


[<ReactComponent>]
let CreateEntityPage () =
    let isCreatedSnackbarOpened, setIsCreatedSnackbarOpened = React.useState(false)
    let handleSenseCreated (sense: Sense) =
        async {
            let eid = Guid.NewGuid() |> EntityId
            let senseString = sense |> Sense.format
            let! result = MindApiImpl.mindApi.Remember(eid, senseString)
            match result with
            | Ok () -> setIsCreatedSnackbarOpened true
            | Error reason -> eprintfn $"Failed create entity: {reason}"
        }
        |> Async.startSafe
    let handleCreatedSnackbarClosed () =
        setIsCreatedSnackbarOpened false
    React.fragment [
        EntityCreateForm handleSenseCreated
        Mui.snackbar [
            snackbar.open' isCreatedSnackbarOpened
            snackbar.onClose (fun _ -> handleCreatedSnackbarClosed ())
            snackbar.autoHideDuration 6000
            snackbar.message "Created!"
            prop.onClick (fun _ -> handleCreatedSnackbarClosed ())
        ]
    ]
