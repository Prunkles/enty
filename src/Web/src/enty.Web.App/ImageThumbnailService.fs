namespace enty.Web.App

open System.Text
open Fable.Core

type IImageThumbnailUrlProvider =
    abstract GetThumbnailUrl:
        sourceUrl: string * ?width: int * ?height: int
            -> string

type BaseAddressImageThumbnailUrlProvider(baseAddress: string) =
    interface IImageThumbnailUrlProvider with
        member this.GetThumbnailUrl(sourceUrl, width, height) =
            let sb = StringBuilder()
            let param name value format =
                match value with
                | Some value -> sb.Append("&" + name + "=" + format value) |> ignore
                | None -> ()

            let sourceUrlEncoded = JS.encodeURIComponent sourceUrl
            sb.Append("?url=" + sourceUrlEncoded) |> ignore

            param "width" width string
            param "height" height string
            param "quality" (Some "100") string

            let queryPart = sb.ToString()
            $"{baseAddress}/fit{queryPart}"

module ImageThumbnailServiceImpl =

    let imageThumbnail: IImageThumbnailUrlProvider =
        upcast BaseAddressImageThumbnailUrlProvider("/image-thumbnail")
