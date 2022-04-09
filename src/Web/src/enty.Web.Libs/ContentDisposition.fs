namespace Fable.ContentDisposition

open Fable.Core
open Fable.Core.JsInterop

type ContentDisposition =
    abstract ``type``: string
    abstract parameter: obj

module private Import =
    let contentDisposition: obj = importDefault "content-disposition"

module ContentDisposition =
    let parse (contentDispositionHeader: string) : ContentDisposition = Import.contentDisposition?parse(contentDispositionHeader)
