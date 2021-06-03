module enty.WebApp.EntityCreating

open System
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open Feliz
open Feliz.MaterialUI
open Fable.SimpleHttp
open enty.Core
open MindApiImpl

[<ReactComponent>]
let SenseInput () =
    let input, setInput = React.useState("")
    Mui.textareaAutosize [
        prop.value input
        prop.onChange setInput
    ]

let writeResource formData = async {
    let ridG = Guid.NewGuid()
    let! response =
        Http.request $"/storage/{ridG}"
        |> Http.content (BodyContent.Form formData)
        |> Http.send
    if response.statusCode = 200 then
        return Ok ridG
    else
        return Error ()
}

let ImageEntityForm () =
    let selectedFile, setSelectedFile = React.useState(null)
    let changeHandler (event: Event) =
        let file = event.target?files?(0)
        setSelectedFile file
    let handleSubmission () =
        async {
            let formData =
                FormData.create ()
                |> FormData.appendFile "File" selectedFile
            let! result = writeResource formData

            ()
        } |> Async.StartImmediate
    Html.div [
        Mui.formLabel [ formLabel.children "Original" ]
        Mui.input [
            input.type' "file"
            input.onChange changeHandler
        ]
        Mui.button [
            prop.onClick (ignore >> handleSubmission)
            prop.text "Submit"
        ]
    ]

let CreateEntity () =
//    let senseString, setSenseString = React.useState("")
//    let onConfirm () =
//        let eid = EntityId (Guid.NewGuid())
//        mindApi.Remember(eid, senseString) |> Async.StartImmediate
    Mui.paper [
        ImageEntityForm ()
//        Mui.grid [
//            grid.container true
//            prop.children [
//                Mui.grid [
//                    grid.item true
//                    prop.children [
//                        Mui.textareaAutosize [
//                            prop.value senseString
//                            prop.onChange setSenseString
//                        ]
//                        Mui.button [
//                            prop.text "Create"
//                            prop.onClick (fun _ -> onConfirm ())
//                        ]
//                    ]
//                ]
//                Mui.grid [
//                    grid.item true
//                    prop.children [
//                        Html.input [
//                            prop.type'.file
//
//                        ]
//                    ]
//                ]
//            ]
//        ]
    ]
