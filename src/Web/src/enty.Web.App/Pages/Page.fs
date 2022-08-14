namespace enty.Web.App.Pages

open FsToolkit.ErrorHandling
open Feliz.Router
open enty.Core
open enty.Mind.Client.Fable


type WishPageInitials =
    { WishString: string
      PageNumber: int
      WishOrderingKey: WishOrderingKey
      WishOrderingDescending: bool }

[<RequireQualifiedAccess>]
type Page =
    | Index
    | CreateEntity
    | EditEntity of EntityId
    | Wish of WishPageInitials option
    | Entity of EntityId
    | DainselfButton
    | NotFound

[<RequireQualifiedAccess>]
module Page =

    let parsePath path =
        match path with
        | [] -> Page.Index
        | [ "create-entity" ] -> Page.CreateEntity
        | [ "edit-entity"; Route.Guid eid ] -> Page.EditEntity (EntityId eid)
        | [ "wish"; Route.Query [ "w", wishString; "p", Route.Int page; "ok", ok; "od", od ] ] ->
            option {
                let! orderingKey = match ok with "id" -> Some WishOrderingKey.ById | "created" -> Some WishOrderingKey.ByCreation | "updated" -> Some WishOrderingKey.ByUpdated | _ -> None
                let! orderingDescending = match od with "0" -> Some false | "1" -> Some true | _ -> None
                let initials =
                    { WishString = wishString
                      PageNumber = page
                      WishOrderingKey = orderingKey
                      WishOrderingDescending = orderingDescending }
                return Page.Wish (Some initials)
            }
            |> Option.defaultValue Page.NotFound
        | [ "wish" ] -> Page.Wish None
        | [ "entity"; Route.Guid eid ] -> Page.Entity (EntityId eid)
        | [ "dainself-button" ] -> Page.DainselfButton
        | _ -> Page.NotFound

    let formatPath (page: Page) : string =
        match page with
        | Page.Index -> Router.formatPath()
        | Page.CreateEntity -> Router.formatPath("create-entity")
        | Page.EditEntity (EntityId eid) -> Router.formatPath("edit-entity", string eid)
        | Page.Wish None -> Router.formatPath("wish")
        | Page.Wish (Some initials) ->
            Router.formatPath("wish", [
                "w", initials.WishString
                "p", string initials.PageNumber
                "ok", match initials.WishOrderingKey with WishOrderingKey.ById -> "id" | WishOrderingKey.ByCreation -> "created" | WishOrderingKey.ByUpdated -> "updated"
                "od", if initials.WishOrderingDescending then "1" else "0"
            ])
        | Page.Entity (EntityId eid) -> Router.formatPath("entity", string eid)
        | Page.DainselfButton -> Router.formatPath("dainself-button")
        | Page.NotFound -> Router.formatPath("not-found")
