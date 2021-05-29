module enty.WebApp.Program

open System
open Fable.Core
open Feliz
open Feliz.MaterialUI
open enty.Core
open enty.WebApp

open EntityCreating



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
let WishPage () =
    let entities, setEntities = React.useState([| |])
    let onWishString wishString =
        async {
            let! eids, total = MindApiImpl.mindApi.Wish(wishString, 0, 10)
            let! entities = MindApiImpl.mindApi.GetEntities(eids)
            setEntities entities
        } |> Async.StartImmediate
    Html.div [
        yield WishInput onWishString true
        for entity in entities do
            yield EntityRendering.Entity entity
    ]

[<ReactComponent>]
let App () =
    Mui.container [
        CreateEntity ()
    ]

open Browser.Dom

ReactDOM.render(App, document.getElementById("app"))
