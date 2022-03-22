module enty.WebApp.EntityCreating

open System
open System.Text
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
open MindApiImpl


module Seq =

    let trySequentiallyCompose (funs: ('a -> 'a) seq) : ('a -> 'a) option =
        Seq.fold (fun s x -> Some ^ match s with None -> x | Some f -> f >> x) None funs

module Result =

    let allIsOk (results: Result<'a, 'e> list) : Result<'a list, 'e> =
        ((None: Result<'a list, 'e> option), results) ||> List.fold (fun s r ->
            match s with
            | None ->
                match r with
                | Ok x -> Ok [x]
                | Error e -> Error e
            | Some s ->
                match s with
                | Error e -> Error e
                | Ok s ->
                    match r with
                    | Ok x -> Ok [ yield! s; yield x ]
                    | Error e -> Error e
            |> Some
        )
        |> Option.get

    // let allIsOk (results: Result<'a, 'e> seq) : Result<'a seq, 'e> =
    //     ((None: Result<'a seq, 'e> option), results) ||> Seq.fold (fun s r ->
    //         match s with
    //         | None ->
    //             match r with
    //             | Ok x -> Ok (seq { x })
    //             | Error e -> Error e
    //         | Some s ->
    //             match s with
    //             | Error e -> Error e
    //             | Ok s ->
    //                 match r with
    //                 | Ok x -> Ok (seq { yield! s; yield x })
    //                 | Error e -> Error e
    //         |> Some
    //     )
    //     |> Option.get

[<RequireQualifiedAccess>]
module Sense =

    let private isValueSimple (value: string) =
        value
        |> Seq.forall ^fun c ->
            Char.IsLetter(c)
            || Char.IsDigit(c)
            || c = '-' || c = '_'

    let format (sense: Sense) : string =
        let sb = StringBuilder()
        let rec printSense (sb: StringBuilder) sense =
            match sense with
            | Sense.Value v ->
                if isValueSimple v
                then sb.Append(v) |> ignore
                else sb.Append('"').Append(v).Append('"') |> ignore
            | Sense.List l ->
                sb.Append('[') |> ignore
                sb.Append(' ') |> ignore
                for e in l do
                    printSense sb e
                    sb.Append(' ') |> ignore
                sb.Append(']') |> ignore
            | Sense.Map m ->
                sb.Append('{') |> ignore
                sb.Append(' ') |> ignore
                for KeyValue (k, v) in m do
                    sb.Append(k).Append(' ') |> ignore
                    printSense sb v
                    sb.Append(' ') |> ignore
                sb.Append('}') |> ignore
        printSense sb sense
        sb.ToString()

    let formatMultiline (sense: Sense) : string =
        let rec appendSense (sb: StringBuilder) (indent: int) (sense: Sense) =
            let append (s: string) = sb.Append(s) |> ignore
            let appendLineIndent (s: string) = sb.AppendLine(s).Append(String(' ', 4 * indent)) |> ignore
            let appendLineIndentIndented (s: string) = sb.AppendLine(s).Append(String(' ', 4 * (indent + 1))) |> ignore
            let appendSenseIndented sense = appendSense sb (indent + 1) sense
            // let appendSenses (separator: string) senses =
            //     match senses with
            //     | head :: _ -> appendSenseIndented head | _ -> ()
            //     match senses with
            //     | _ :: tail ->
            //         for sense in tail do
            //             append separator
            //             appendSenseIndented sense
            //     | _ -> ()
            match sense with
            | Sense.Value value ->
                if isValueSimple value
                then sb.Append(value) |> ignore
                else sb.Append('"').Append(value).Append('"') |> ignore
            | Sense.List list ->
                append "["
                for value in list do
                    appendLineIndentIndented ""
                    appendSenseIndented value
                appendLineIndent ""
                append "]"
            | Sense.Map map ->
                append "{"
                for KeyValue (key, value) in map do
                    appendLineIndentIndented ""
                    append key
                    append " "
                    appendSenseIndented value
                appendLineIndent ""
                append "}"
        let sb = StringBuilder()
        appendSense sb 0 sense
        sb.ToString()

// ----


type IResourceStorage =
    abstract Create: formData: FormData -> Async<Result<Uri, string>>

type ResourceStorage(baseUrl: string) =
    interface IResourceStorage with
        member _.Create(formData) = async {
            let rid = Guid.NewGuid()
            let! response =
                Http.request $"{baseUrl}/{string rid}"
                |> Http.method POST
                |> Http.content (BodyContent.Form formData)
                |> Http.send
            if response.statusCode = 200 then
                return Ok (Uri($"{baseUrl}/{string rid}"))
            else
                return Error response.responseText
        }

let resourceStorage: IResourceStorage = ResourceStorage("http://localhost:5020")

type ImageShapeUrlStatus =
    | Empty
    | Set of Uri
    | Invalid
    | Loading

[<ReactComponent>]
let ImageSenseShapeForm (senseChanged: Result<Sense, string> -> unit) =
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
            senseChanged (Ok sense)
        | ImageShapeUrlStatus.Invalid -> senseChanged (Error "Invalid URI")
        | ImageShapeUrlStatus.Empty -> senseChanged (Error "No URI")
        | _ -> ()
        setState s
    let onUrlChanged (urlInput: string) =
        setUrlInput urlInput
        match Uri.TryCreate(urlInput, UriKind.Absolute) with
        | true, uri -> setState (ImageShapeUrlStatus.Set uri)
        | false, _ -> setState ImageShapeUrlStatus.Invalid
    Mui.paper [
        Mui.textField [
            textField.label "URL"
            textField.variant.outlined
            textField.onChange onUrlChanged
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
                            let! result = resourceStorage.Create(formData)
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
let TagSenseShapeForm (senseChanged: Result<Sense, string> -> unit) =
    let tags, setTags = React.useState([])
    let onTagInputChange (input: string) =
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
        senseChanged (Ok sense)
    Mui.paper [
        Mui.grid [
            grid.container true
            grid.direction.column
            grid.children [
                Mui.textField [
                    textField.label "Tags"
                    textField.onChange onTagInputChange
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
let SenseShapeSelector (forms: (SenseShapeFormId * string) list) (formSelected: SenseShapeFormId -> bool -> unit) =
    Mui.formGroup [
        for formId, formName in forms do
            Mui.formControlLabel [
                let switchElement =
                    Mui.switch [
                        switch.onChange (fun (active: bool) -> formSelected formId active)
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
    let onFormSelected formId active =
        if active then dispatch (EntityCreateForm.Msg.SelectForm formId) else dispatch (EntityCreateForm.Msg.DeselectForm formId)
    let onFormSenseChanged (formId: SenseShapeFormId) (sense: Result<_, _>) =
        dispatch (EntityCreateForm.Msg.FormSenseChanged (formId, sense))
    Mui.grid [
        grid.container true
        grid.children [
            Mui.grid [
                grid.item true
                grid.xs._2
                grid.children [
                    let forms = [ for form in forms -> form.Id, form.Name ]
                    SenseShapeSelector forms onFormSelected
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
                                            formElement (onFormSenseChanged formId)
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
