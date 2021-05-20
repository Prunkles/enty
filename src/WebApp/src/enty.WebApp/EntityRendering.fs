module enty.WebApp.EntityRendering

open Fable.React
open Feliz
open Feliz.MaterialUI
open enty.Core

type IEntityRenderer =
    abstract TryRender: Entity -> ReactElement

let tryRenderImagePreviewEntity (entity: Entity) : ReactElement option =
    failwith ""

let renderEntity (entity: Entity) : ReactElement =
    failwith ""

[<ReactComponent>]
let ImageEntity (entity: Entity) =
    Html.img [
        let (EntityId eidG) = entity.Id
        prop.src $"/storage/{eidG}"
        prop.alt $"{eidG}"
    ]

[<ReactComponent>]
let ImagePreviewEntity (EntityId eidG) =
    Html.img [
        prop.src $"/storage/{eidG}"
        prop.alt $"{eidG}"
    ]
