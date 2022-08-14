module enty.Web.App.Program

open System
open Browser
open Fable.Core

open Feliz
open Feliz.Router
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open enty.Core
open enty.Web.App.Pages
open enty.Web.App.Utils

[<ReactComponent>]
let AppBarHeaderButton (name: string) (page: Page) =
    let url = React.useMemo(fun () ->
        page |> Page.formatPath
    , [| page |])
    Mui.button [
        button.component' "button"
        prop.sx {|
            my=2; color="white"; display="block"
            textAlign = "center"
        |}
        prop.text name
        prop.onClick (fun event ->
            event.preventDefault()
            Router.navigatePath(url)
        )
        button.href url
    ]

[<ReactComponent>]
let AppBar () =
    Mui.appBar [
        appBar.position.static'
        appBar.children [
            Mui.container [
                container.maxWidth.xl
                container.children [
                    Mui.toolbar [
                        toolbar.disableGutters true
                        toolbar.children [
                            Mui.box [
                                prop.sx {|
                                    mr = 5
                                    display = "flex"
                                |}
                                box.children [
                                    Html.img [
                                        prop.src "/enty.svg"
                                        prop.style [ style.filter.invert 100; style.cursor "pointer" ]
                                        prop.onClick (fun _ -> Router.navigatePath())
                                    ]
                                ]
                            ]
                            Mui.box [
                                box.sx {| flexGrow=1; display ="flex" |}
                                box.children [
                                    AppBarHeaderButton "Create" Page.CreateEntity
                                    AppBarHeaderButton "Wish" (Page.Wish None)
                                    AppBarHeaderButton "Dainself Button" Page.DainselfButton
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

[<ReactComponent>]
let App () =
    let currentPath = Router.currentPath()
    printfn $"currentPath: {currentPath}"
    let page, setPage = React.useState(currentPath |> Page.parsePath)
    React.router [
        router.pathMode
        router.onUrlChanged (Page.parsePath >> setPage)
        router.children [
            Mui.cssBaseline []
            AppBar ()
            Mui.container [
                Mui.box @+ [
                    box.sx {|
                        pt = 2
                        pb = 4
                    |}
                ] <| [
                    match page with
                    | Page.Index -> Html.h1 "Index"
                    | Page.CreateEntity -> CreateEntityPage ()
                    | Page.EditEntity eid -> EditEntityPage.EditEntityPage eid
                    | Page.Wish initials ->
                        WishPage {| Initials = initials |}
                    | Page.Entity eid -> EntityPage eid
                    | Page.DainselfButton -> DainselfButton.DainselfButton ()
                    | Page.NotFound -> Html.h1 "Not found"
                ]
            ]
        ]
    ]

async {
    do! ResourceStorageHardcodeImpl.init ()
    ReactDOM.render(App, Dom.document.getElementById("app"))
}
|> Async.startSafe
