module enty.WebApp.Program

open System
open Fable
open Fable.Core
open Browser.Types
open Feliz
open Feliz.MaterialUI
open enty.Core
open enty.Mind.Client.Fable

[<ReactComponent>]
let WishInput onConfirm clearOnConfirm =
    let content, setContent = React.useState("")
    let confirm () =
        onConfirm content
        if clearOnConfirm then setContent ""
    let confirmButton =
        Mui.button [
            prop.text "Search"
            prop.onClick (fun _ -> confirm ())
        ]
    Mui.textField [
        textField.value content
        textField.variant.outlined
        textField.label "Wish"
        textField.InputProps [
            input.endAdornment confirmButton
        ]
        prop.onKeyUp(key.enter, fun _ -> confirm ())
        textField.onChange setContent
    ]

[<ReactComponent>]
let App () =
    let entityIds, setEntityIds = React.useState([| |])
    let wishString strWish =
        promise {
            let! ids = MindService.wish strWish 0 10
            setEntityIds ids
        } |> Promise.start
    Mui.container [
        WishInput wishString false
        Html.div [
            for entityId in entityIds ->
                Html.div [
                    prop.text $"%A{entityId}"
                ]
        ]
    ]

open Browser.Dom

printfn $"{TestDefine.runtime}: %A{TestDefine.result}"

ReactDOM.render(App, document.getElementById("app"))
