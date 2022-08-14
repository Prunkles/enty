[<AutoOpen>]
module enty.Web.App.Pages.WishPage

open System
open Browser
open Elmish
open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5
open Feliz.Router
open Feliz.UseElmish

open enty.Utils
open enty.Core
open enty.Mind.Client.Fable
open enty.Web.App
open enty.Web.App.Utils

[<RequireQualifiedAccess>]
module WishPage =

    [<RequireQualifiedAccess>]
    type Msg =
        | WishInputChanged of string
        | WishOrderingKeyChanged of WishOrderingKey
        | WishOrderingDescendingChanged of bool
        | PageNumberChanged of int
        | WishSearch
        | WishSucceed of Entity list * total: int
        | WishFailed of error: string

    [<RequireQualifiedAccess>]
    type Status =
        | Empty
        | Loading
        | Error of string
        | Loaded of Entity list * total: int

    type State =
        { WishInput: string option
          WishOrderingKey: WishOrderingKey
          WishOrderingDescending: bool

          PageSize: int
          PageNumber: int

          Status: Status }

    let search (state: State) : Cmd<Msg> =
        let wishString = state.WishInput.Value
        Cmd.ofAsyncDispatch ^fun dispatch -> async {
            let ordering = { Key = state.WishOrderingKey; Descending = state.WishOrderingDescending }
            let offset = (state.PageNumber - 1) * state.PageSize
            let limit = state.PageSize
            let! wishResult = MindApiImpl.mindApi.Wish(wishString, ordering, offset, limit)
            match wishResult with
            | Ok (entityIds, total) ->
                let! entities = MindApiImpl.mindApi.GetEntities(entityIds)
                let entities =
                    let entitiesMap = entities |> Seq.map (fun e -> e.Id, e) |> Map.ofSeq
                    entityIds
                    |> Seq.map (fun eid -> entitiesMap.[eid])
                    |> Seq.toList

                dispatch (Msg.WishSucceed (entities, total))

                let url =
                    let initials = { WishString = wishString; PageNumber = state.PageNumber; WishOrderingKey = state.WishOrderingKey; WishOrderingDescending = state.WishOrderingDescending }
                    Page.Wish (Some initials) |> Page.formatPath
                Dom.window.history.pushState(null, "", url)

            | Error error ->
                dispatch (Msg.WishFailed error)
        }

    let init (initials: WishPageInitials option) : State * Cmd<Msg> =
        let pageSize = 12
        match initials with
        | None ->
            { WishInput = None
              WishOrderingKey = WishOrderingKey.ByUpdated
              WishOrderingDescending = true
              PageSize = pageSize
              PageNumber = 1
              Status = Status.Empty }
            , Cmd.none
        | Some initials ->
            let state =
                { WishInput = Some initials.WishString
                  WishOrderingKey = WishOrderingKey.ByUpdated
                  WishOrderingDescending = true
                  PageSize = pageSize
                  PageNumber = initials.PageNumber
                  Status = Status.Empty }
            state, search state

    let update (msg: Msg) (state: State) : State * Cmd<Msg> =
        match msg with
        | Msg.WishInputChanged input ->
            let state = { state with WishInput = if input = String.Empty then None else Some input }
            state, Cmd.none
        | Msg.WishOrderingKeyChanged orderingKey ->
            let state = { state with WishOrderingKey = orderingKey }
            state, Cmd.none
        | Msg.WishOrderingDescendingChanged orderingDescending ->
            let state = { state with WishOrderingDescending = orderingDescending }
            state, Cmd.none

        | Msg.PageNumberChanged pageNumber ->
            let state = { state with PageNumber = pageNumber }
            let cmd = search state
            state, cmd

        | Msg.WishSearch ->
            let state = { state with Status = Status.Loading }
            let state = { state with PageNumber = 1 }
            let cmd = search state
            state, cmd

        | Msg.WishFailed error ->
            let state = { state with Status = Status.Error error }
            state, Cmd.none

        | Msg.WishSucceed (entities, total) ->
            let state = { state with Status = Status.Loaded (entities, total) }
            state, Cmd.none

let boxx = Operators.box

type private Msg = WishPage.Msg
type private Status = WishPage.Status

[<ReactComponent>]
let WishPage (props: {| Initials: WishPageInitials option |}) =
    let state, dispatch = React.useElmish(WishPage.init, WishPage.update, props.Initials, [| boxx props |])

    let handleWishInputChanged (wishInput: string) =
        dispatch (Msg.WishInputChanged wishInput)

    let handleSearch () =
        dispatch Msg.WishSearch

    let handleOrderingKeyInputChanged input =
        let key =
            match input with
            | "updated" -> WishOrderingKey.ByUpdated
            | "created" -> WishOrderingKey.ByCreation
            | "id" -> WishOrderingKey.ById
            | _ -> unreachable
        dispatch (Msg.WishOrderingKeyChanged key)
    let orderingKeyInput = React.useMemo(fun () ->
        match state.WishOrderingKey with
        | WishOrderingKey.ByUpdated -> "updated"
        | WishOrderingKey.ByCreation -> "created"
        | WishOrderingKey.ById -> "id"
    , [| boxx state.WishOrderingKey |])

    let handleOrderingDescendingChanged descending =
        dispatch (Msg.WishOrderingDescendingChanged descending)

    let handleThumbnailClicked (entity: Entity) =
        Router.navigatePath(Page.formatPath (Page.Entity entity.Id))

    let handlePageNumberChanged (pageNumber: int) =
        dispatch (Msg.PageNumberChanged pageNumber)

    Mui.stack @+ [
        stack.spacing 3
    ] <| [
        Mui.box @+ [ prop.sx {| display = "flex"; flexDirection = "row"; gap = 1 |} ] <| [
            Mui.box @+ [ prop.sx {| flexGrow = 1 |} ] <| [
                Mui.textField [
                    textField.value (state.WishInput |> function Some i -> i | None -> "")
                    textField.fullWidth true
                    textField.variant.outlined
                    textField.label "Wish"
                    textField.InputProps [
                        input.endAdornment (
                            Mui.button [
                                prop.text "Search"
                                prop.onClick (fun _ -> handleSearch ())
                                match state.WishInput with
                                | None -> prop.disabled true
                                | _ -> ()
                            ]
                        )
                    ]
                    prop.onKeyUp(key.enter, fun _ -> handleSearch ())
                    textField.onChange handleWishInputChanged
                    match state.Status with
                    | Status.Error error ->
                        textField.error true
                        textField.helperText error
                    | _ -> ()
                ]
            ]
            Mui.select @+ [
                select.label "Order by"
                select.value orderingKeyInput
                select.onChange (fun (e: string) -> handleOrderingKeyInputChanged e)
            ] <| [
                Mui.menuItem [
                    prop.value "updated"
                    prop.text "Updated"
                ]
                Mui.menuItem [
                    prop.value "created"
                    prop.text "Created"
                ]
                Mui.menuItem [
                    prop.value "id"
                    prop.text "Id"
                ]
            ]
            Mui.formControlLabel [
                formControlLabel.label "Descending"
                formControlLabel.control (
                    Mui.checkbox [
                        checkbox.checked' state.WishOrderingDescending
                        checkbox.onChange handleOrderingDescendingChanged
                    ]
                )
            ]
        ]
        match state.Status with
        | Status.Empty -> Html.text "There's nothing there yet"

        | Status.Loading ->
            Mui.circularProgress []

        | Status.Loaded (entities, total) ->
            Mui.box @+ [
                prop.sx {|
                    display = "flex"
                    justifyContent = "center"
                    gap = 2
                    flexWrap = "wrap"
                |}
            ] <| [
                for entity in entities ->
                    Html.a @+ [
                        prop.style [
                            style.textDecoration.none
                            style.cursor.pointer
                        ]
                        let href = Page.Entity entity.Id |> Page.formatPath
                        prop.href href
                        prop.onClick (fun event ->
                            event.preventDefault()
                            handleThumbnailClicked entity
                        )
                    ] <| [
                        Mui.box [
                            box.sx {|
                                height = 300
                                width = 250
                            |}
                            prop.key (entity.Id |> EntityId.Unwrap)
                            box.children [
                                EntityThumbnail.EntityThumbnail entity
                            ]
                        ]
                    ]
            ]
            Mui.pagination [
                let pageCount = ceil (float total / float state.PageSize) |> int
                pagination.siblingCount 2
                pagination.count pageCount
                pagination.page state.PageNumber
                pagination.onChange (fun _ p -> handlePageNumberChanged p)
            ]

        | Status.Error _ ->
            ()
    ]
