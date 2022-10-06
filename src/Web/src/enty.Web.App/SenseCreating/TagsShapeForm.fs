module enty.Web.App.SenseCreating.TagsShapeForm

open System
open FsToolkit.ErrorHandling

open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open enty.Core
open enty.Utils
open enty.Web.App.Utils
open enty.Web.App.SenseShapes
open enty.Web.App.SenseParsing
open enty.Web.App.SenseFormatting
open enty.Web.App.Components


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
                Ok tags
            | Error error ->
                Error error
            | Unreachable x -> x
        , [| Operators.box tagsInput |])

    React.useEffect(fun () ->
        match tags with
        | Ok tags ->
            let sense =
                senseMap {
                    "tags", Sense.List tags
                }
            onSenseChanged (Validation.ok sense)
        | Error error ->
            onSenseChanged (Validation.error $"%A{error}")
    , [| Operators.box tags |])

    Mui.stack @+ [
        stack.direction.column
        stack.spacing 1
    ] <| [
        Mui.stack @+ [
            prop.style [
                style.alignItems.baseline
            ]
            stack.direction.row
            stack.spacing 1
        ] <| [
            let inline bracket (symbol: string) =
                Mui.typography [
                    prop.text symbol
                    typography.color.textSecondary
                    typography.variant.h5
                    prop.style [
                        style.userSelect.none
                    ]

                ]
            Mui.textField [
                textField.label "Tags"
                textField.fullWidth true
                textField.value tagsInput
                textField.onChange setTagsInput
                match tags with
                | Error error ->
                    textField.error true
                    textField.helperText (SenseParseErrorComp error)
                | _ -> ()
                textField.InputProps [
                    input.startAdornment (
                        Mui.inputAdornment [
                            inputAdornment.position.start
                            inputAdornment.children (bracket "[")
                        ]
                    )
                    input.endAdornment (
                        Mui.inputAdornment [
                            inputAdornment.position.end'
                            inputAdornment.children (bracket "]")
                        ]
                    )
                ]
            ]
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
