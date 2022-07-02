[<AutoOpen>]
module enty.Web.App.Pages.WishPage

open System
open Browser
open Browser.Types
open Elmish
open Fable.Core
open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open Feliz.Router
open enty.Core

open enty.Web.App
open enty.Web.App.Utils

type input' = input
type box' = box


[<ReactComponent>]
let WishInput (initialInput: string option) onConfirm clearOnConfirm =
    let input, setInput = React.useState(initialInput |> Option.defaultValue "")
    let confirm () =
        onConfirm input
        if clearOnConfirm then setInput ""
    let confirmButton =
        Mui.button [
            prop.text "Search"
            prop.onClick (fun _ -> confirm ())
        ]
    Mui.textField [
        textField.value input
        textField.variant.outlined
        textField.label "Wish"
        textField.InputProps [
            input'.endAdornment confirmButton
        ]
        prop.onKeyUp(key.enter, fun _ -> confirm ())
        textField.onChange setInput
    ]

[<RequireQualifiedAccess>]
type WishPageStatus =
    | Empty
    | Loading
    | Entities of Entity array * total: int

type WishPageInitials =
    { WishString: string
      PageNumber: int }

let inline box' x = FSharp.Core.Operators.box x

[<ReactComponent>]
let WishPage (props: {| Initials: WishPageInitials option |}) =
    let pageSize = 12
    let pageNumber, setPageNumber = React.useState(props.Initials |> Option.map (fun i -> i.PageNumber) |> Option.defaultValue 1)
    let status, setStatus = React.useState(WishPageStatus.Empty)
    let wishInput, setWishInput = React.useState(props.Initials |> Option.map (fun i -> i.WishString))

    let handleWishStringEntered (pageNumber: int) (wishString: string) =
        async {
            setWishInput (Some wishString)
            setStatus WishPageStatus.Loading
            let! wishResult = MindApiImpl.mindApi.Wish(wishString, (pageNumber - 1) * pageSize, pageSize)
            match wishResult with
            | Ok (eids, total) ->
                let! entities = MindApiImpl.mindApi.GetEntities(eids)
                let url = Page.Wish (Some (wishString, pageNumber)) |> Page.formatPath
                Dom.window.history.pushState(null, "", url)
                setStatus (WishPageStatus.Entities (entities, total))
            | Error reason ->
                setStatus WishPageStatus.Empty
                eprintfn $"Failed wish: {reason}"
        } |> Async.startSafe

    let handlePageNumberChanged (pageNumber: int) =
        setPageNumber pageNumber
        match wishInput with
        | Some wishInput -> handleWishStringEntered pageNumber wishInput
        | _ -> ()

    React.useEffect(fun () ->
        match props.Initials with
        | None -> ()
        | Some initials ->
            handleWishStringEntered pageNumber initials.WishString
            setPageNumber initials.PageNumber
        ()
    , [| box' props.Initials |])

    let handleThumbnailClicked (entity: Entity) =
        Router.navigatePath(Page.formatPath (Page.Entity entity.Id))
        printfn $"Entity clicked: {entity}"

    Mui.stack @+ [
        stack.spacing 3
    ] <| [
        WishInput wishInput (handleWishStringEntered pageNumber) false
        match status with
        | WishPageStatus.Empty -> Html.text "There's nothing there yet"
        | WishPageStatus.Loading ->
            Mui.circularProgress []
        | WishPageStatus.Entities (entities, total) ->
            Mui.box @+ [
                prop.sx {|
                    display = "flex"
                    justifyContent = "center"
                    gap = 2
                    flexWrap = "wrap"
                |}
            ] <| [
                for entity in entities ->
                    Mui.box [
                        box.sx {|
                            height = 300
                            width = 250
                        |}
                        box.children [
                            EntityThumbnail.EntityThumbnail entity (fun () -> handleThumbnailClicked entity)
                        ]
                    ]
            ]
            Mui.pagination [
                pagination.count (total / pageSize + 1)
                pagination.page pageNumber
                pagination.onChange (fun _ p -> handlePageNumberChanged p)
            ]
    ]
