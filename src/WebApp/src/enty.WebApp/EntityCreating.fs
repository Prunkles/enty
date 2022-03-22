module enty.WebApp.EntityCreating

open System
open System.Text.RegularExpressions

open Fable.Core
open Fable.Core.JsInterop
open Fable.SimpleHttp
open Browser.Types

open Elmish
open Feliz
open Feliz.UseElmish
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open enty.Core
open enty.Utils

open enty.WebApp
open enty.WebApp.Utils
open enty.WebApp.SenseFormatting
open enty.WebApp.MindApiImpl


type ImageShapeUrlStatus =
    | Empty
    | Set of Uri
    | Invalid
    | Loading

[<ReactComponent>]
let ImageSenseShapeForm (onSenseChanged: Result<Sense, string> -> unit) =
    let urlInput, setUrlInput = React.useState("")
    let state, setState = React.useState(ImageShapeUrlStatus.Empty)
    let setState s =
        match s with
        | ImageShapeUrlStatus.Set uri ->
            let sense =
                senseMap {
                    "image", senseMap {
                        "uri", string uri
                    }
                }
            onSenseChanged (Ok sense)
        | ImageShapeUrlStatus.Invalid ->
            onSenseChanged (Error "Invalid URI")
        | ImageShapeUrlStatus.Empty ->
            onSenseChanged (Error "No URI")
        | _ -> ()
        setState s
    let handleUrlInputChanged (urlInput: string) =
        setUrlInput urlInput
        match Uri.TryCreate(urlInput, UriKind.Absolute) with
        | true, uri -> setState (ImageShapeUrlStatus.Set uri)
        | false, _ -> setState ImageShapeUrlStatus.Invalid
    Mui.paper [
        Mui.textField [
            textField.label "URL"
            textField.variant.outlined
            textField.onChange handleUrlInputChanged
            textField.value urlInput
            match state with
            | ImageShapeUrlStatus.Invalid ->
                textField.error true
                textField.helperText "Invalid URI"
            | ImageShapeUrlStatus.Loading ->
                textField.disabled true
            | _ -> ()
        ]
        Mui.button [
            button.variant.contained
            button.component' "label"
            button.children [
                Html.text "File"
                Html.input [
                    input.type' "file"
                    prop.hidden true
                    prop.onChange (fun (e: Event) ->
                        let selectedFile: Browser.Types.File = e.target?files?(0)
                        let formData =
                            FormData.create ()
                            |> FormData.appendNamedFile "File" selectedFile.name selectedFile
                        async {
                            setState ImageShapeUrlStatus.Loading
                            let! result = ResourceStorageHardcodeImpl.resourceStorage.Create(formData)
                            match result with
                            | Ok uri ->
                                setState (ImageShapeUrlStatus.Set uri)
                            | Error reason ->
                                setState ImageShapeUrlStatus.Empty
                                eprintfn $"{reason}"
                        }
                        |> Async.startSafe
                    )
                ]
            ]
        ]
    ]

// TODO: Remove quotes
[<ReactComponent>]
let TagSenseShapeForm (onSenseChanged: Result<Sense, string> -> unit) =
    let tags, setTags = React.useState([])
    let handleTagInputChange (input: string) =
        let tags =
            // TODO: Wait https://github.com/fable-compiler/Fable/issues/2845 fix
            // // (?<=")(?:\\\\|\\"|.)*(?=")|[A-Za-z0-9_-]+
            // Regex.Matches(input, @"(?<="")(?:\\\\|\\""|.)*(?="")|[A-Za-z0-9_-]+")
            // |> Seq.map ^fun m -> m.Value
            // |> Seq.toList
            input.Split(" ", StringSplitOptions.RemoveEmptyEntries) |> Array.toList
        setTags tags
        let sense =
            senseMap {
                "tags", senseList {
                    yield! tags
                }
            }
        onSenseChanged (Ok sense)
    Mui.paper [
        Mui.grid [
            grid.container true
            grid.direction.column
            grid.children [
                Mui.textField [
                    textField.label "Tags"
                    textField.onChange handleTagInputChange
                ]
                Mui.stack [
                    stack.direction.row
                    stack.children [
                        for tag in tags do
                            Mui.chip [
                                chip.label tag
                                chip.variant.outlined
                            ]
                    ]
                ]
            ]
        ]
    ]

[<ReactComponent>]
let SenseFormatter (sense: Sense) =
    Html.pre (Sense.formatMultiline sense)

type SenseShapeFormId = SenseShapeFormId of int

type SenseShapeFormElement = (Result<Sense, string> -> unit) -> ReactElement

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

module EntityCreateForm =

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
        printfn $"update | %A{msg} | %A{state}"
        match msg with
        | Msg.SelectForm formId ->
            { state with ActiveForms = state.ActiveForms |> Map.add formId (Error $"Form {formId} has no sense yet") }
            , Cmd.none
        | Msg.DeselectForm formId ->
            { state with ActiveForms = state.ActiveForms |> Map.remove formId }
            , Cmd.none
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
    [
        "Image", ImageSenseShapeForm
        "Tags", TagSenseShapeForm
    ]
    |> Seq.indexed
    |> Seq.map ^fun (idx, (name, element)) -> { Id = SenseShapeFormId idx; Name = name; Element = element }
    |> Seq.toList

[<ReactComponent>]
let EntityCreateForm () =
    let state, dispatch = React.useElmish(EntityCreateForm.init, EntityCreateForm.update, forms)
    let handleFormSelected formId active =
        if active then dispatch (EntityCreateForm.Msg.SelectForm formId) else dispatch (EntityCreateForm.Msg.DeselectForm formId)
    let handleFormSenseChanged (formId: SenseShapeFormId) (sense: Result<_, _>) =
        dispatch (EntityCreateForm.Msg.FormSenseChanged (formId, sense))
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
                                for { Id = formId; Element = formElement } in activeForms do
                                    Html.div [
                                        prop.key (formId |> fun (SenseShapeFormId x) -> x)
                                        prop.children [
                                            formElement (handleFormSenseChanged formId)
                                        ]
                                    ]
                                match state.Sense with
                                | Ok sense -> SenseFormatter sense
                                | _ -> ()

                                Mui.button [
                                    button.variant.contained
                                    match state.Sense with
                                    | Ok sense ->
                                        prop.onClick (fun _ ->
                                            printfn $"%A{sense}"
                                        )
                                    | Error reason ->
                                        button.disabled true
                                    button.children "Create"
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
