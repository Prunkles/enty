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
        let h = elementRef.current.Value.clientHeight
        let w = elementRef.current.Value.clientWidth
        setSizes (Some (int h, int w))
    )
    elementRef, sizes


[<ReactComponent>]
let EntityThumbnail (entity: Entity) =
    let sizingElementRef, sizes = useElementSize ()
    Mui.paper @+ [
        prop.sx {|
            height = "100%"
        |}
    ] <| [
        Mui.stack @+ [ prop.sx {| height = "100%" |}  ] <| [
            MuiE.boxSx {| p = 1; flexGrow = 1 |} @+ [] <| [
                match entity.Sense with
                | Apply ImageSenseShape.parse (Some imageSense) ->
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
                        | Some (height, width) ->
                            let thumbnailUrl =
                                ImageThumbnailServiceImpl.imageThumbnail.GetThumbnailUrl(
                                    imageSense.Uri,
                                    height = height, width = width
                                )
                            Html.img [
                                prop.src thumbnailUrl
                                prop.style [
                                    style.maxHeight.minContent
                                    style.maxWidth.minContent
                                ]
                            ]
                    ]
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
