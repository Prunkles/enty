module enty.Web.App.SenseCreating.ImageShapeForm

open System
open FsToolkit.ErrorHandling

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

open enty.Utils
open enty.Core
open enty.Web.App
open enty.Web.App.Utils
open enty.Web.App.SenseShapes


[<RequireQualifiedAccess>]
module ImageSenseShapeForm =

    type ImageMetadata =
        { ContentType: string option
          ContentLength: int option
          Filename: string option }

    [<RequireQualifiedAccess>]
    type Msg =
        | UrlInputChanged of string
        | FileSelected of File
        | FileUploaded of File * Uri
        | ImageMetadataReceived of ImageMetadata * Uri

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

    open global.Fetch

    let getImageMetadata (uri: string) = async {
        let fetchData method = async {
            let! response =
                tryFetch uri [
                    RequestProperties.Method method
                ]
                |> Async.AwaitPromise
            match response with
            | Ok response ->
                let contentType = response.Headers.ContentType
                let contentLength = response.Headers.ContentLength |> Option.map int
                let contentDisposition = response.Headers.ContentDisposition
                let filename: string option =
                    contentDisposition
                    |> Option.bind ^fun contentDisposition ->
                        try Some (ContentDisposition.parse contentDisposition).parameters?filename
                        with _ -> None
                return Ok { ContentType = contentType; ContentLength = contentLength; Filename = filename }
            | Error ex -> return Error ex
        }
        let fetchDataHeadMerging metadata = async {
            let! resultGet = fetchData HttpMethod.GET
            match resultGet with
            | Error ex -> return Error ex
            | Ok metadata' ->
                let metadata = {
                    ContentType = Option.orElse metadata'.ContentType metadata.ContentType
                    ContentLength = Option.orElse metadata'.ContentLength metadata.ContentLength
                    Filename = Option.orElse metadata'.Filename metadata.Filename
                }
                return Ok metadata
        }
        let! resultHead = fetchData HttpMethod.HEAD
        match resultHead with
        | Error _ ->
            return! fetchDataHeadMerging { ContentType = None; ContentLength = None; Filename = None }
        | Ok ({ ContentType = None } | { ContentLength = None } | { Filename = None } as metadata) ->
            return! fetchDataHeadMerging metadata
        | Ok metadata ->
            return Ok metadata
    }

    let update (onSenseChanged: Validation<Sense, string> -> unit) (msg: Msg) (state: State) : State * Cmd<Msg> =
        match msg with
        | Msg.UrlInputChanged input ->
            let uri = Uri.TryCreate(input, UriKind.Absolute) |> Option.ofTryByref
            let uriHeadCmd = Cmd.ofAsyncDispatch ^fun dispatch -> async {
                match uri with
                | Some uri ->
                    match! getImageMetadata (string uri) with
                    | Ok metadata ->
                        if metadata.ContentType.IsNone then printfn "WRN: Couldn't fetch image metadata ContentType"
                        if metadata.ContentLength.IsNone then printfn "WRN: Couldn't fetch image metadata ContentLength"
                        if metadata.Filename.IsNone then printfn "WRN: Couldn't fetch image metadata Filename"
                        dispatch (Msg.ImageMetadataReceived (metadata, uri))
                    | Error ex ->
                        printfn $"WRN: Failed fetch image metadata: {ex}"
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
        | Msg.ImageMetadataReceived (imageMetadata, uri) ->
            let sense = senseMap {
                "image", senseMap {
                    "resource", senseMap {
                        "uri", string uri
                        match imageMetadata.ContentType with Some contentType -> "content-type", contentType | _ -> ()
                        match imageMetadata.ContentLength with Some contentLength -> "content-length", string contentLength | _ -> ()
                        match imageMetadata.Filename with Some filename -> "filename", filename | _ -> ()
                    }
                    "size", "TODO"
                }
            }
            { state with Status = Status.Valid uri; UrlInput = string uri }
            , Cmd.ofSub (fun _ -> onSenseChanged (Ok sense))
        | Msg.FileUploaded (file, uri) ->
            let sense = senseMap {
                "image", senseMap {
                    "resource", senseMap {
                        "uri", string uri
                        "content-type", file.``type``
                        "content-length", string file.size
                        "filename", file.name
                    }
                    "size", "TODO"
                }
            }
            { state with Status = Status.Valid uri; UrlInput = string uri }
            , Cmd.ofSub (fun _ -> onSenseChanged (Ok sense))


[<ReactComponent>]
let ImageSenseShapeForm (initialSense: Sense) (onSenseChanged: Validation<Sense, string> -> unit) =
    let state, dispatch = React.useElmish(ImageSenseShapeForm.init, ImageSenseShapeForm.update onSenseChanged, initialSense)
    let selectFiles (ev: Event) =
        let selectedFile: Browser.Types.File = ev.target?files?(0)
        dispatch (ImageSenseShapeForm.Msg.FileSelected selectedFile)
    let changeUrlInput (input: string) =
        dispatch (ImageSenseShapeForm.Msg.UrlInputChanged input)

    Mui.box [
        Mui.stack @+ [ stack.direction.column; stack.spacing 1 ] <| [
            MuiE.stackRow @+ [ ] <| [
                Mui.textField [
                    prop.sx {| flexGrow = 1 |}
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
                Mui.stack @+ [
                    prop.sx {| p = 1 |}
                    stack.direction.row
                    stack.spacing 1
                    stack.alignItems.center
                ] <| [
                    Mui.typography [
                        prop.style [ style.userSelect.none ]
                        prop.text "Load from file"
                    ]
                    Mui.button @+ [ button.variant.outlined; button.component' "label" ] <| [
                        Html.text "Choose"
                        Html.input [
                            input.type' "file"
                            prop.hidden true
                            prop.onChange selectFiles
                        ]
                    ]
                ]
            ]
            match state.Status with
            | ImageSenseShapeForm.Status.Valid uri ->
                Mui.box @+ [] <| [
                    Html.img [
                        prop.src (string uri)
                        prop.style [
                            style.maxHeight (length.px 600)
                            style.maxWidth (length.perc 100)
                        ]
                    ]
                ]
            | _ -> ()
        ]
    ]
