namespace enty.Web.App.Pages

open System
open Feliz.Router
open enty.Core

[<RequireQualifiedAccess>]
type Page =
    | Index
    | CreateEntity
    | EditEntity of EntityId
    | Wish of (string * int) option
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
        | [ "wish"; Route.Query [ "wish", wishString; "page", Route.Int page ] ] -> Page.Wish (Some (wishString, page))
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
        | Page.Wish (Some (wishString, page)) -> Router.formatPath("wish", [ "wish", wishString; "page", string page ])
        | Page.Entity (EntityId eid) -> Router.formatPath("entity", string eid)
        | Page.DainselfButton -> Router.formatPath("dainself-button")
        | Page.NotFound -> Router.formatPath("not-found")
