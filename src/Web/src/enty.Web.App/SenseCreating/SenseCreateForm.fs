module enty.Web.App.SenseCreating.SenseCreateForm

open Elmish
open Feliz
open Feliz.UseElmish
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open enty.Core
open enty.Utils

open enty.Web.App.Utils
open enty.Web.App.SenseParsing
open enty.Web.App.SenseFormatting
open enty.Web.App.SenseCreating.ImageShapeForm
open enty.Web.App.SenseCreating.TagsShapeForm


[<ReactComponent>]
let SenseFormatter (sense: Sense) =
    Html.pre (Sense.formatMultiline sense)

type SenseShapeFormId = SenseShapeFormId of int

type SenseShapeFormElement = Sense -> (Result<Sense, string> -> unit) -> ReactElement

type SenseShapeForm =
    { Id: SenseShapeFormId
      Element: SenseShapeFormElement
      Name: string }

[<ReactComponent>]
let SenseShapeSelector (forms: (SenseShapeFormId * string) list) (onFormSelected: SenseShapeFormId -> bool -> unit) =
    Mui.formGroup [
        for formId, formName in forms do
            Mui.formControlLabel [
                let switchElement =
                    Mui.switch [
                        switch.onChange (fun (active: bool) -> onFormSelected formId active)
                    ]
                formControlLabel.control switchElement
                formControlLabel.label formName
            ]
    ]

module SenseCreateForm =

    [<RequireQualifiedAccess>]
    type Msg =
        | SelectForm of SenseShapeFormId
        | DeselectForm of SenseShapeFormId
        | FormSenseChanged of SenseShapeFormId * Result<Sense, string>

    type State =
        { Forms: SenseShapeForm list
          ActiveForms: Map<SenseShapeFormId, Result<Sense, string>>
          Sense: Result<Sense, string> }

    let init forms =
        { Forms = forms
          ActiveForms = Map.empty
          Sense = Error "Empty sense" }
        , Cmd.none

    let update (msg: Msg) (state: State) : State * Cmd<Msg> =
        match msg with
        | Msg.SelectForm formId ->
            { state with ActiveForms = state.ActiveForms |> Map.add formId (Error $"Form {formId} has no sense yet") }
            , Cmd.none
        | Msg.DeselectForm formId ->
            let activeForms = state.ActiveForms |> Map.remove formId
            let resultSense =
                activeForms
                |> Map.values
                |> Seq.toList
                |> Result.allIsOk
                |> Result.map (List.reduce Sense.merge)
            { state with ActiveForms = activeForms; Sense = resultSense }, Cmd.none
        | Msg.FormSenseChanged (formId, sense) ->
            let forms = state.ActiveForms |> Map.add formId sense
            let resultSense =
                forms
                |> Map.values
                |> Seq.toList
                |> Result.allIsOk
                |> Result.map (List.reduce Sense.merge)
            { state with ActiveForms = forms; Sense = resultSense }, Cmd.none


let forms =
    let data: (string * SenseShapeFormElement) list = [
        "Image", ImageSenseShapeForm
        "Tags", TagsSenseShapeForm
    ]
    data
    |> Seq.indexed
    |> Seq.map ^fun (idx, (name, element)) -> { Id = SenseShapeFormId idx; Name = name; Element = element }
    |> Seq.toList

[<ReactComponent>]
let SenseCreateForm (onCreated: Sense -> unit) (initialSense: Sense) (finalButtonText: string) =
    let state, dispatch = React.useElmish(SenseCreateForm.init, SenseCreateForm.update, forms)
    let handleFormSelected formId active =
        if active then dispatch (SenseCreateForm.Msg.SelectForm formId) else dispatch (SenseCreateForm.Msg.DeselectForm formId)
    let handleFormSenseChanged (formId: SenseShapeFormId) (sense: Result<_, _>) =
        dispatch (SenseCreateForm.Msg.FormSenseChanged (formId, sense))
    let handleCreateButtonClicked (sense: Sense) =
        onCreated sense
    Mui.grid [
        grid.container true
        grid.children [
            Mui.grid [
                grid.item true
                grid.xs._2
                grid.children [
                    let forms = [ for form in forms -> form.Id, form.Name ]
                    SenseShapeSelector forms handleFormSelected
                ]
            ]
            Mui.grid [
                grid.item true
                grid.xs._10
                grid.children [
                    Html.div [
                        Mui.stack [
                            stack.direction.column
                            stack.spacing 2
                            stack.children [
                                let activeForms =
                                    state.ActiveForms
                                    |> Map.keys
                                    |> Seq.map ^fun formId ->
                                        state.Forms |> List.find (fun f -> f.Id = formId)
                                for { Id = formId; Name = formName; Element = formElement } in activeForms do
                                    Mui.card [
                                        prop.key (formId |> fun (SenseShapeFormId x) -> x)
                                        card.children [
                                            Mui.cardHeader [
                                                cardHeader.title formName
                                            ]
                                            Mui.cardContent [
                                                formElement initialSense (handleFormSenseChanged formId)
                                            ]
                                        ]
                                    ]
                                match state.Sense with
                                | Ok sense -> SenseFormatter sense
                                | _ -> ()

                                Mui.button [
                                    button.variant.contained
                                    match state.Sense with
                                    | Ok sense ->
                                        prop.onClick (fun _ -> handleCreateButtonClicked sense)
                                    | Error reason ->
                                        button.disabled true
                                    button.children finalButtonText
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
