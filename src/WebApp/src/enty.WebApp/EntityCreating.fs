module enty.WebApp.EntityCreating

open System
open Feliz
open Feliz.MaterialUI
open enty.Core
open MindApiImpl

[<ReactComponent>]
let SenseInput () =
    let input, setInput = React.useState("")
    Mui.textareaAutosize [
        
    ]

let ImageEntityForm () =
    Html.div [
        Mui.formLabel [ formLabel.children "Original" ]
        Mui.input [
            input.type' "file"
        ]
    ]

let TraitSelection () =
    
    ()

let CreateEntity () =
//    let senseString, setSenseString = React.useState("")
//    let onConfirm () =
//        let eid = EntityId (Guid.NewGuid())
//        mindApi.Remember(eid, senseString) |> Async.StartImmediate
    Mui.paper [
        ImageTraitSelection ()
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
