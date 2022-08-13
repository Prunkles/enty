module enty.Web.App.EntityThumbnail

open Browser.Types
open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open enty.Utils
open enty.Core
open enty.Web.App.Utils
open enty.Web.App.SenseShapes

[<RequireQualifiedAccess>]
module Map =

    let (|Item|_|) (key: 'k) (map: Map<'k, 'v>) : 'v option = Map.tryFind key map

let (|Apply|) f x = f x

[<ReactComponent>]
let EntityIdHint (entityId: EntityId) =
    MuiE.boxSx {| display = "flex"; justifyContent = "center" |} @+ [] <| [
        Mui.typography [
            let (EntityId eidS) = entityId
            typography.variant.caption
            typography.component' "pre"
            typography.children $"{eidS}"
        ]
    ]

[<Hook>]
let useElementSize () =
    let sizes, setSizes = React.useState(None)
    let elementRef = React.useRef(None: HTMLElement option)
    React.useLayoutEffectOnce(fun () ->
        let newSizes =
            match elementRef.current with
            | Some elementRef ->
                let w = elementRef.clientWidth
                let h = elementRef.clientHeight
                Some (w, h)
            | None -> None
        if newSizes <> sizes then
            setSizes newSizes
    )
    elementRef, sizes

[<ReactComponent>]
let ImageEntityThumbnailImage (imageSense: ImageSenseShape) =
    let sizingElementRef, sizes = useElementSize ()
    Mui.stack @+ [
        prop.sx {|
            height = "100%"
            alignItems = "center"
            justifyContent = "center"
        |}
    ] <| [
        match sizes with
        | None ->
            Html.div [
                prop.style [
                    style.height (length.perc 100)
                    style.width (length.perc 100)
                ]
                prop.ref sizingElementRef
            ]
        | Some (width, height) ->
            let thumbnailUrl =
                ImageThumbnailServiceImpl.imageThumbnail.GetThumbnailUrl(
                    imageSense.Uri,
                    width = int width, height = int height
                )
            Html.img [
                prop.src thumbnailUrl
                prop.style [
                    style.maxHeight.minContent
                    style.maxWidth.minContent
                ]
            ]
    ]

[<ReactComponent>]
let EntityThumbnail (entity: Entity) =
    Mui.paper @+ [
        prop.sx {|
            height = "100%"
        |}
    ] <| [
        Mui.stack @+ [ prop.sx {| height = "100%" |}  ] <| [
            MuiE.boxSx {| p = 1; flexGrow = 1 |} @+ [] <| [
                match entity.Sense with
                | Apply ImageSenseShape.parse (Some imageSense) ->
                    ImageEntityThumbnailImage imageSense
                | _ ->
                    MuiE.boxSx {| display = "flex"; alignItems = "center"; justifyContent = "center"; height = "100%" |} @+ [] <| [
                        Mui.typography [
                            typography.children "Undefined sense shape"
                        ]
                    ]
            ]
            EntityIdHint entity.Id
        ]
    ]
