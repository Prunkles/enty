module enty.Web.App.Pages.CreateEntityImageBatchPage

open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open Fable.SimpleHttp

open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open enty.Core
open enty.Web.App
open enty.Web.App.Utils

type State =
    | Empty
    | Selected of File array

[<ReactComponent>]
let CreateEntityImageBatchPage () =
    let files, setFiles = React.useState(State.Empty)

    let handleFilesSelected (files: File array) =
        setFiles (State.Selected files)

    let handleBatchCreateClicked (files: File array) =
        async {
            for file in files do
                let form =
                    FormData.create ()
                    |> FormData.appendNamedFile "" file.name file
                let! result = ResourceStorageHardcodeImpl.resourceStorage.Create(form)
                match result with
                | Ok uri ->
                    let sense = senseMap {
                        "image", senseMap {
                            "resource", senseMap {
                                "uri", string uri
                            }
                        }
                    }
                    ()
                | Error reason ->
                    Browser.Dom.window.alert($"Failed upload file: {reason}")
        }
        |> Async.startSafe

    Mui.box [
        box.children [
            Mui.button [
                button.variant.outlined
                button.component' "label"
                button.children [
                    Html.text "Select images"
                    Html.input [
                        input.type' "file"
                        prop.hidden true
                        prop.onChange (fun (e: Event) ->
                            let files: File array = e.target?files
                            handleFilesSelected files
                        )
                    ]
                ]
            ]
            match files with
            | State.Empty -> ()
            | State.Selected files ->
                Mui.box [
                    box.children [
                        for file in files ->
                            Html.span file.name
                    ]
                ]
                Mui.button [
                    prop.text "Batch create"
                    prop.onClick (fun _ -> handleBatchCreateClicked files)
                ]
        ]
    ]
