module enty.Web.App.SenseCreating.TagsShapeForm

open System
open FsToolkit.ErrorHandling

open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open enty.Core
open enty.Web.App.Utils
open enty.Web.App.SenseShapes
open enty.Web.App.SenseParsing
open enty.Web.App.SenseFormatting


[<ReactComponent>]
let TagsSenseShapeForm (initialSense: Sense) (onSenseChanged: Validation<Sense, string> -> unit) =
    let tagsInput, setTagsInput =
        React.useState(fun () ->
            match TagsSenseShape.parse initialSense with
            | Some tagsShape ->
                tagsShape.Tags |> Seq.map Sense.format |> fun ts -> String.Join(" ", ts)
            | _ -> ""
        )

    let tags =
        React.useMemo(fun () ->
            let res = Sense.parse $"[ %s{tagsInput} ]"
            match res with
            | Ok (Sense.List tags) ->
                Validation.ok tags
            | Error error | Const "Result is not a list, pretty unreachable" error ->
                Validation.error error
        , [| Operators.box tagsInput |])

    React.useEffect(fun () ->
        match tags with
        | Ok tags ->
            let sense =
                senseMap {
                    "tags", Sense.List tags
                }
            onSenseChanged (Ok sense)
        | Error error ->
            onSenseChanged (Error error)
    , [| Operators.box tags |])

    Mui.stack @+ [
        stack.direction.column
        stack.spacing 1
    ] <| [
        Mui.textField [
            textField.label "Tags"
            textField.value tagsInput
            textField.onChange setTagsInput
            match tags with
            | Error errors ->
                textField.error true
                textField.helperText (
                    Html.pre [
                        prop.style [
                            style.margin 0
                        ]
                        prop.text $"{errors}"
                    ]
                )
            | _ -> ()
        ]
        match tags with
        | Ok tags ->
            Mui.stack @+ [
                stack.direction.row
                stack.spacing 0.5
            ] <| [
                for tag in tags do
                    Mui.chip [
                        chip.label (Sense.format tag)
                        chip.variant.outlined
                    ]
            ]
        | _ -> ()
    ]
