module enty.Web.App.EntityThumbnail

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
                    Mui.stack @+ [
                        prop.sx {| height = "100%" |}
                    ] <| [
                        MuiE.boxSx {|
                            backgroundImage = $"url(\"%s{imageSense.Uri}\")"
                            backgroundSize = "contain"
                            backgroundRepeat = "no-repeat"
                            backgroundPosition = "center"
                            height = "100%"
                        |} [
                            prop.src imageSense.Uri
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
