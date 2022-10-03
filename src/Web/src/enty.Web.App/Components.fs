module enty.Web.App.Components

open System
open Feliz

open enty.Web.App.Utils
open enty.Web.App.SenseParsing

[<ReactComponent>]
let SenseParseErrorComp parseError =
    let loc = parseError.Location
    let input =
        if parseError.Input.Length = loc
        then parseError.Input + " "
        else parseError.Input
    let d = 8
    let sym = input.[loc]
    let allPrev = input |> Seq.take loc |> Seq.toArray
    let allPost = input |> Seq.skip (loc + 1) |> Seq.toArray
    Html.span @+ [
        prop.style [
            style.whitespace.pre
            style.fontFamily "Roboto Mono"
        ]
    ] <| [
        Html.span "'"
        if allPrev.Length > d then
            Html.span "…"
            Html.span (String(allPrev).Substring(allPrev.Length - d, d))
        else
            Html.span (String(allPrev))
        Html.span [
            prop.style [
                style.textDecoration.underline
                style.textDecorationStyle.wavy
                style.custom("text-decoration-skip-ink", "none")
            ]
            prop.text (string sym)
        ]
        if allPost.Length > d then
            Html.span (String(allPost).Substring(0, d))
            Html.span "…"
        else
            Html.span (String(allPost))

        Html.span $"': %A{parseError.Kind}"
    ]
