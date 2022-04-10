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

// [<RequireQualifiedAccess>]
// module WishPage =
//
//     type Msg =
//         | A
//
//     type State =
//         { PageSize: int }
//
//     let init () =
//         ()
//
//     let update (msg: Msg) (state: State) : State * Cmd<Msg> =
//         match msg with
//         | Msg.A -> state, Cmd.none


[<ReactComponent>]
let WishPage (props: {| Initials: WishPageInitials option |}) =
    let pageSize = 1
    let pageNumber, setPageNumber = React.useState(props.Initials |> Option.map (fun i -> i.PageNumber) |> Option.defaultValue 0)
    let status, setStatus = React.useState(WishPageStatus.Empty)
    let wishInput, setWishInput = React.useState(props.Initials |> Option.map (fun i -> i.WishString))

    let handleWishStringEntered (wishString: string) =
        async {
            setWishInput (Some wishString)
            setStatus WishPageStatus.Loading
            let! wishResult = MindApiImpl.mindApi.Wish(wishString, pageNumber * pageSize, pageSize)
            match wishResult with
            | Ok (eids, total) ->
                let! entities = MindApiImpl.mindApi.GetEntities(eids)

                // Router.navigatePath("wish", ["wish", wishString; "page", string (total / pageSize)])
                // Router.navigatePath(Page.Wish (Some (wishString, pageNumber)) |> Page.formatPath)
                setStatus (WishPageStatus.Entities (entities, total))
            | Error reason ->
                setStatus WishPageStatus.Empty
                eprintfn $"Failed wish: {reason}"
        } |> Async.startSafe

    let handlePageNumberChanged (pageNumber: int) =
        setPageNumber pageNumber
        // match wishInput with
        // | Some wishInput -> handleWishStringEntered wishInput
        // | _ -> ()

    // React.useEffect(fun () ->
    //     match props.Initials with
    //     | None -> ()
    //     | Some initials ->
    //         handleWishStringEntered initials.WishString
    //         setPageNumber initials.PageNumber
    //     ()
    // , [| box' props.Initials |])

    let handleThumbnailClicked (entity: Entity) =
        printfn $"Entity clicked: {entity}"

    Mui.stack [
        stack.spacing 3
        stack.children [
            WishInput wishInput handleWishStringEntered false
            match status with
            | WishPageStatus.Empty -> Html.text "There's nothing there yet"
            | WishPageStatus.Loading ->
                Mui.circularProgress []
            | WishPageStatus.Entities (entities, total) ->
                Mui.box [
                    prop.sx {|
                        display = "flex"
                        justifyContent = "center"
                        gap = 2
                        flexWrap = "wrap"
                    |}
                    box.children [
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
                ]
                Mui.pagination [
                    pagination.count (total / pageSize)
                    pagination.page pageNumber
                    pagination.onChange handlePageNumberChanged
                    pagination.renderItem (fun item ->
                        Html.none
                    )
                ]
        ]
    ]
