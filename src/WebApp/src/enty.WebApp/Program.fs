module enty.WebApp.Program

open System
open Fable.Core
open Feliz
open Feliz.MaterialUI
open enty.Core
open enty.Mind.Client.Fable
open enty.Mind.Server.Api

open MindServiceImpl


[<ReactComponent>]
let EntityList (entities: Entity seq) =
    Html.div []


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
    let eids, setEids = React.useState([| |])
    let stringWish wishString =
        printfn "Search..."
        async {
            try
                let! eids, total = mindService.Wish(wishString, 0, 10)
                setEids eids
            with ex ->
                eprintfn $"ERR: {ex}"
        } |> Async.StartImmediate
    Mui.container [
        WishInput stringWish false
        Html.div [
            for entityId in eids ->
                Html.div [
                    prop.text $"%A{entityId}"
                ]
        ]
    ]

open Browser.Dom

ReactDOM.render(App, document.getElementById("app"))
