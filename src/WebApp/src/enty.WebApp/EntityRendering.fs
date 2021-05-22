module enty.WebApp.EntityRendering

open Feliz
open Feliz.MaterialUI
open enty.Utils
open enty.Core
open enty.Core.Traits
open enty.WebApp.MindServiceImpl


[<RequireQualifiedAccess>]
module Image =

    [<ReactComponent>]
    let EntityElement (sourceEid: EntityId) =
        let previewEid, setPreviewEid = React.useState(None)
        let fetchPreviewId () = async {
            let wishString = sprintf """{ image:preview:link "%s" }""" (let (EntityId eidG) = sourceEid in string eidG)
            let! eids, total = mindService.Wish(wishString, 0, 1)
            setPreviewEid (Some eids.[0])
        }
        React.useEffect(fetchPreviewId >> Async.StartImmediate, [| box sourceEid |])
        match previewEid with
        | Some (EntityId previewEidG) ->
            Html.img [
                prop.src $"/storage/{previewEidG}"
                prop.alt $"{previewEidG}"
            ]
        | None ->
            Mui.circularProgress [ ]
    
    [<ReactComponent>]
    let Entity (eid: EntityId) =
        let (EntityId eidG) = eid
        Html.img [
            prop.src $"/storage/{eidG}"
            prop.alt $"{eidG}"
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
        option {
            let! isImage = Sense.Image.isImage entity.Sense
            if not isImage then return! None else
            return Image.EntityElement entity.Id
        }
    ]
    |> Option.defaultWith (fun () -> Undefined.EntityElement entity)

[<ReactComponent>]
let Entity (entity: Entity) =
    Option.choose [
        
    ]
    |> Option.defaultWith (fun () -> Undefined.Entity entity)
