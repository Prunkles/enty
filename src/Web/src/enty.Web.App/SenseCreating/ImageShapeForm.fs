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

    type UrlStatus =
        | Loading
        | Valid of Uri
        | Invalid

    [<RequireQualifiedAccess>]
    type Msg =
        | UrlInputChanged of string
        | UriValidated of UrlStatus
        | FileSelected of File
        | FileUploaded of Uri
        | ImageSizesReceived of width: int * height: int
        | ImageMetadataReceived of ImageMetadata

    type State =
        { Url: UrlStatus
          Metadata: ImageMetadata option
          ImageSize: (int * int) option
          UrlInput: string }

    let init (sense: Sense) =
        let cmd =
            match ImageSenseShape.parse sense with
            | Some imageShape -> Cmd.ofMsg (Msg.UrlInputChanged imageShape.Uri)
            | None -> Cmd.none
        { Url = UrlStatus.Invalid
          Metadata = None
          ImageSize = None
          UrlInput = String.Empty }
        , cmd

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

    let update (msg: Msg) (state: State) : State * Cmd<Msg> =
        match msg with
        | Msg.UrlInputChanged input ->
            let uriHeadCmd = Cmd.ofAsyncDispatch ^fun dispatch -> async {
                let uri = Uri.TryCreate(input, UriKind.Absolute) |> Option.ofTryByref
                match uri with
                | Some uri ->
                    match! getImageMetadata (string uri) with
                    | Ok metadata ->
                        if metadata.ContentType.IsNone then printfn "WRN: Couldn't fetch image metadata ContentType"
                        if metadata.ContentLength.IsNone then printfn "WRN: Couldn't fetch image metadata ContentLength"
                        if metadata.Filename.IsNone then printfn "WRN: Couldn't fetch image metadata Filename"
                        dispatch (Msg.UriValidated (UrlStatus.Valid uri))
                        dispatch (Msg.ImageMetadataReceived metadata)
                    | Error ex ->
                        dispatch (Msg.UriValidated UrlStatus.Invalid)
                        // printfn $"WRN: Failed fetch image metadata: {ex}"
                | None -> ()
            }
            { state with UrlInput = input }
            , uriHeadCmd
        | Msg.UriValidated urlStatus ->
            { state with Url = urlStatus }, Cmd.none
        | Msg.ImageSizesReceived (width, height) ->
            let state = { state with ImageSize = Some (width, height) }
            state, Cmd.none
        | Msg.FileSelected file ->
            let imageMetadata =
                { ContentType = Some file.``type``
                  ContentLength = Some file.size
                  Filename = Some file.name }
            let state = { state with Metadata = Some imageMetadata }
            let uploadCmd =
                Cmd.ofAsyncDispatch ^fun dispatch -> async {
                    let formData =
                        FormData.create ()
                        |> FormData.appendNamedFile "File" file.name file
                    let! result = ResourceStorageHardcodeImpl.resourceStorage.Create(formData)
                    match result with
                    | Ok uri ->
                        dispatch (Msg.FileUploaded uri)
                    | Error reason ->
                        eprintfn $"{reason}"
                }
            state, uploadCmd
        | Msg.ImageMetadataReceived imageMetadata ->
            let state = { state with Metadata = Some imageMetadata }
            state, Cmd.none
        | Msg.FileUploaded uri ->
            let state = { state with Url = UrlStatus.Valid uri; UrlInput = string uri }
            state, Cmd.none


[<ReactComponent>]
let ImageSenseShapeForm (initialSense: Sense) (onSenseChanged: Validation<Sense, string> -> unit) =
    let state, dispatch = React.useElmish(ImageSenseShapeForm.init, ImageSenseShapeForm.update, initialSense)
    let selectFiles (ev: Event) =
        let selectedFile: Browser.Types.File = ev.target?files?(0)
        dispatch (ImageSenseShapeForm.Msg.FileSelected selectedFile)
    let changeUrlInput (input: string) =
        dispatch (ImageSenseShapeForm.Msg.UrlInputChanged input)

    let handleImageLoaded (width: int) (height: int) =
        dispatch (ImageSenseShapeForm.Msg.ImageSizesReceived (width, height))

    React.useEffect(fun () ->
        match state.Url with
        | ImageSenseShapeForm.UrlStatus.Valid uri ->
            let sense = senseMap {
                "image", senseMap {
                    "resource", senseMap {
                        "uri", string uri
                        match state.Metadata with
                        | Some imageMetadata ->
                            match imageMetadata.ContentType with Some contentType -> "content-type", contentType | _ -> ()
                            match imageMetadata.ContentLength with Some contentLength -> "content-length", string contentLength | _ -> ()
                            match imageMetadata.Filename with Some filename -> "filename", filename | _ -> ()
                        | None -> ()
                    }
                    match state.ImageSize with
                    | Some (width, height) ->
                        "size", senseMap {
                            "width", string width
                            "height", string height
                        }
                    | None -> ()
                }
            }
            onSenseChanged (Ok sense)
        | _ ->
            onSenseChanged (Validation.error $"Invalid URI: {state.UrlInput}")
    , [| state :> obj |])

    Mui.box [
        Mui.stack @+ [ stack.direction.column; stack.spacing 1 ] <| [
            MuiE.stackRow @+ [ ] <| [
                Mui.textField [
                    prop.sx {| flexGrow = 1 |}
                    textField.label "URI"
                    textField.variant.outlined
                    textField.value state.UrlInput
                    textField.onChange changeUrlInput
                    match state.Url with
                    | ImageSenseShapeForm.UrlStatus.Invalid ->
                        textField.error true
                        textField.helperText "Invalid URI"
                    | ImageSenseShapeForm.UrlStatus.Loading ->
                        textField.disabled true
                    | ImageSenseShapeForm.UrlStatus.Valid _ ->
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
            match state.Url with
            | ImageSenseShapeForm.UrlStatus.Valid uri ->
                Mui.box @+ [] <| [
                    Html.img [
                        prop.src (string uri)
                        prop.style [
                            style.maxHeight (length.px 600)
                            style.maxWidth (length.perc 100)
                        ]
                        prop.onLoad (fun event ->
                            let img = (event.target :?> HTMLImageElement)
                            handleImageLoaded (int img.naturalWidth) (int img.naturalHeight)
                        )
                    ]
                ]
            | _ -> ()
        ]
    ]
