module enty.WebApp.EntityRendering

open Feliz
open Feliz.MaterialUI
open enty.Core


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

[<ReactComponent>]
let UndefinedEntity { Id = EntityId eidG } =
    Html.div [
        prop.text $"Entity {eidG}"
    ]


[<ReactComponent>]
let Entity (entity: Entity) =
    ()
