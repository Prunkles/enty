namespace enty.ImagePreviewService.Client.Fable

open System.Text
open Fable.Core

type Resampler =
    | Bicubic | Nearest | Box | Mitchell | Catmull
    | Lanczos2 | Lanczos3 | Lanczos5 | Lanczos8
    | Welch | Robidoux | RobidouxSharp | Spline | Triangle | Hermite
    member this.ToQueryParam() =
        match this with
        | Bicubic -> "bicubic" | Nearest -> "nearest" | Box -> "box" | Mitchell -> "mitchell"
        | _ -> failwith "TODO"

type ResizeMode =
    | Crop
    member this.ToQueryParam() =
        match this with
        | Crop -> "crop"

type IImagePreviewUrlProvider =
    abstract GetUrl:
        sourceUrl: string * ?width: int * ?height: int *
        ?rmode: ResizeMode * ?rsampler: Resampler * ?rxy: (int * int)
            -> string

type BaseAddressImagePreviewUrlProvider(baseAddress: string) =
    interface IImagePreviewUrlProvider with
        member this.GetUrl(sourceUrl, width, height, rmode, rsampler, rxy) =
            let sb = StringBuilder()
            let param name value format =
                match value with
                | Some value -> sb.Append("&" + name + "=" + format value) |> ignore
                | None -> ()
            
            let sourceUrlEncoded = JS.encodeURIComponent sourceUrl
            sb.Append("?url=" + sourceUrlEncoded) |> ignore
            
            param "width" width string
            param "height" height string
            param "rmode" rmode (fun x -> x.ToQueryParam())
            param "rsampler" rsampler (fun x -> x.ToQueryParam())
            param "rxy" rxy (fun (x, y) -> string x + "," + string y)
            
            let queryPart = sb.ToString()
            baseAddress + queryPart
