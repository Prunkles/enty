module enty.Web.App.SenseCreating.TagsShapeForm

open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open enty.Core
open enty.Web.App.SenseShapes
open enty.Web.App.SenseParsing
open enty.Web.App.SenseFormatting


[<ReactComponent>]
let TagsSenseShapeForm (initialSense: Sense) (onSenseChanged: Result<Sense, string> -> unit) =
    // let tagsInput, setTagsInput =
    //     React.useState(fun () ->
    //         match TagsSenseShape.parse initialSense with
    //         | Some tagsShape ->
    //             tagsShape.Tags |> Seq.map Sense.format |> fun ts -> String.Join(" ", ts)
    //         | _ -> ""
    //     )
    let tags, setTags =
        React.useState(fun () ->
            match TagsSenseShape.parse initialSense with
            | Some tagsShape ->
                tagsShape.Tags
            | None -> []
        )
    let handleTagInputChange (input: string) =
        let res = Sense.parse $"[ %s{input} ]"
        match res with
        | Ok (Sense.List tags) ->
            setTags tags
            let sense =
                senseMap {
                    "tags", Sense.List tags
                }
            onSenseChanged (Ok sense)
        | Error reason | Const "Result is not a list, pretty unreachable" reason ->
            onSenseChanged (Error reason)
    Mui.stack [
        stack.direction.column
        stack.spacing 1
        stack.children [
            Mui.textField [
                textField.label "Tags"
                textField.onChange handleTagInputChange
            ]
            Mui.stack [
                stack.direction.row
                stack.spacing 0.5
                stack.children [
                    for tag in tags do
                        Mui.chip [
                            chip.label (Sense.format tag)
                            chip.variant.outlined
                        ]
                ]
            ]
        ]
    ]
