module enty.Web.App.EntityCreating

open System
open System.Text.RegularExpressions

open Fable.ContentDisposition
open Fable.Core
open Fable.Core.JsInterop
open Fable.SimpleHttp
open Browser.Types

open Elmish
open Feliz
open Feliz.UseElmish
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open Feliz.style
open enty.Core
open enty.Utils

open enty.Web.App
open enty.Web.App.SenseParsing
open enty.Web.App.SenseShapes
open enty.Web.App.Utils
open enty.Web.App.SenseFormatting



[<RequireQualifiedAccess>]
module ImageSenseShapeForm =

    [<RequireQualifiedAccess>]
    type Msg =
        | UrlInputChanged of string
        | FileSelected of File
        | FileUploaded of File * Uri

    type Status =
        | Loading
        | Valid of Uri
        | Invalid

    type State =
        { Status: Status
          UrlInput: string }

    let init (sense: Sense) =
        let cmd =
            match ImageSenseShape.parse sense with
            | Some imageShape -> Cmd.ofMsg (Msg.UrlInputChanged imageShape.Uri)
            | None -> Cmd.none
        { Status = Status.Invalid
          UrlInput = String.Empty }
        , cmd

    let imageSense (uri: Uri) =
        senseMap {
            "resource", senseMap {
                "uri", string uri
            }
        }

    let update (onSenseChanged: Result<Sense, string> -> unit) (msg: Msg) (state: State) : State * Cmd<Msg> =
        match msg with
        | Msg.UrlInputChanged input ->
            let uri = Uri.TryCreate(input, UriKind.Absolute) |> Option.ofTryByref
            let uriHeadCmd = Cmd.ofAsyncDispatch ^fun dispatch -> async {
                match uri with
                | Some uri ->
                    onSenseChanged ^ Ok ^ senseMap {
                        "image", imageSense uri
                    }
                    let! response =
                        Http.request (string uri)
                        |> Http.method HttpMethod.HEAD
                        |> Http.send
                    if response.statusCode = 200 then
                        // Header names to lowercase
                        let headers = response.responseHeaders |> Map.toSeq |> Seq.map (fun (k, v) -> (k.ToLower(), v)) |> Map.ofSeq
                        let contentType = headers |> Map.tryFind "content-type"
                        let contentLength = headers |> Map.tryFind "content-length" |> Option.map int
                        let filename =
                            headers |> Map.tryFind "content-disposition"
                            |> Option.bind ^fun contentDisposition ->
                                Some (ContentDisposition.parse contentDisposition).parameter?filename
                        printfn $"contentType: {contentType}; contentLength: {contentLength}; filename: {filename}"
                        ()
                | None -> ()
            }
            let status =
                match uri with
                | Some uri -> Status.Valid uri
                | None -> Status.Invalid
            { state with Status = status; UrlInput = input }
            , uriHeadCmd
        | Msg.FileSelected file ->
            let uploadCmd =
                Cmd.ofAsyncDispatch ^fun dispatch -> async {
                    let formData =
                        FormData.create ()
                        |> FormData.appendNamedFile "File" file.name file
                    let! result = ResourceStorageHardcodeImpl.resourceStorage.Create(formData)
                    match result with
                    | Ok uri ->
                        dispatch (Msg.FileUploaded (file, uri))
                    | Error reason ->
                        eprintfn $"{reason}"
                }
            state, uploadCmd
        | Msg.FileUploaded (file, uri) ->
            let sense = senseMap {
                "image", senseMap {
                    "resource", senseMap {
                        "uri", string uri
                        "content-length", string file.size
                        "content-type", file.``type``
                    }
                    "size", "TODO"
                }
            }
            { state with Status = Status.Valid uri; UrlInput = string uri }
            , Cmd.ofSub (fun _ -> onSenseChanged (Ok sense))


[<ReactComponent>]
let ImageSenseShapeForm (initialSense: Sense) (onSenseChanged: Result<Sense, string> -> unit) =
    let state, dispatch = React.useElmish(ImageSenseShapeForm.init, ImageSenseShapeForm.update onSenseChanged, initialSense)
    let selectFiles (ev: Event) =
        let selectedFile: Browser.Types.File = ev.target?files?(0)
        dispatch (ImageSenseShapeForm.Msg.FileSelected selectedFile)
    let changeUrlInput (input: string) =
        dispatch (ImageSenseShapeForm.Msg.UrlInputChanged input)

    Mui.box [
        Mui.stack [
            stack.direction.row
            stack.children [
                Mui.stack [
                    Mui.textField [
                        textField.label "URI"
                        textField.variant.outlined
                        textField.value state.UrlInput
                        textField.onChange changeUrlInput
                        match state.Status with
                        | ImageSenseShapeForm.Status.Invalid ->
                            textField.error true
                            textField.helperText "Invalid URI"
                        | ImageSenseShapeForm.Status.Loading ->
                            textField.disabled true
                        | ImageSenseShapeForm.Status.Valid _ ->
                            textField.helperText "Valid URI"
                    ]
                    Mui.stack [
                        stack.direction.row
                        stack.spacing 1
                        stack.alignItems.center
                        stack.children [
                            Mui.typography [
                                prop.style [ userSelect.none ]
                                prop.text "Load from file"
                            ]
                            Mui.button [
                                button.variant.outlined
                                button.component' "label"
                                button.children [
                                    Html.text "Choose"
                                    Html.input [
                                        input.type' "file"
                                        prop.hidden true
                                        prop.onChange selectFiles
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
                Mui.box [
                    box.sx ""
                    box.children [
                        Html.img [
                            match state.Status with
                            | ImageSenseShapeForm.Status.Valid uri ->
                                prop.src (string uri)
                            | _ -> ()
                        ]
                    ]
                ]
            ]
        ]
    ]


[<ReactComponent>]
let TagSenseShapeForm (initialSense: Sense) (onSenseChanged: Result<Sense, string> -> unit) =
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
        "Tags", TagSenseShapeForm
    ]
    data
    |> Seq.indexed
    |> Seq.map ^fun (idx, (name, element)) -> { Id = SenseShapeFormId idx; Name = name; Element = element }
    |> Seq.toList

[<ReactComponent>]
let EntityCreateForm (onCreated: Sense -> unit) (initialSense: Sense) (finalButtonText: string) =
    let state, dispatch = React.useElmish(EntityCreateForm.init, EntityCreateForm.update, forms)
    let handleFormSelected formId active =
        if active then dispatch (EntityCreateForm.Msg.SelectForm formId) else dispatch (EntityCreateForm.Msg.DeselectForm formId)
    let handleFormSenseChanged (formId: SenseShapeFormId) (sense: Result<_, _>) =
        dispatch (EntityCreateForm.Msg.FormSenseChanged (formId, sense))
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
