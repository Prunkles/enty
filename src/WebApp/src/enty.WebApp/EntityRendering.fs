module enty.WebApp.EntityRendering

open Feliz
open Feliz.MaterialUI
open enty.Utils
open enty.Core


[<RequireQualifiedAccess>]
module Image =

    type ImageEntity =
        { MasterSizeUrl: string }
    
    module ImageEntity =
        let parse entity = option {
            let! originalSizeUrl =
                entity.Sense
                |> Sense.tryGetValue [ "image"; "sizes"; "original"; "resource-url" ]
            return { MasterSizeUrl = originalSizeUrl }
        }

    [<ReactComponent>]
    let EntityElement (entity: ImageEntity) =
        let sampleUrl = ImagePreviewServiceImpl.imagePreview.GetUrl(entity.MasterSizeUrl, width=100)
        Html.img [
            prop.src sampleUrl
        ]
    
    [<ReactComponent>]
    let Entity (entity: ImageEntity) =
        Html.img [
            prop.src entity.MasterSizeUrl
        ]


module Undefined =
    
    [<ReactComponent>]
    let EntityElement { Id = EntityId eidG } =
        Html.div [
            prop.text $"Entity {eidG}"
        ]
    
    [<ReactComponent>]
    let Entity { Id = EntityId eidG } =
        Html.div [
            prop.text $"Entity {eidG}"
        ]

[<ReactComponent>]
let EntityElement (entity: Entity) =
    Option.choose [
        entity |> Image.ImageEntity.parse |> Option.map Image.EntityElement
    ]
    |> Option.defaultWith (fun () -> Undefined.EntityElement entity)

[<ReactComponent>]
let Entity (entity: Entity) =
    Option.choose [
        entity |> Image.ImageEntity.parse |> Option.map Image.Entity
    ]
    |> Option.defaultWith (fun () -> Undefined.Entity entity)
