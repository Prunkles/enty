namespace enty.Web.App

open System
open Browser.Types
open Fable.Core.JsInterop
open Fable.SimpleHttp

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

module ResourceStorageHardcodeImpl =
    let resourceStorage: IResourceStorage =
        let baseUrl = emitJsExpr () "process.env.ENTY_STORAGE_ADDRESS"
        ResourceStorage(baseUrl)
