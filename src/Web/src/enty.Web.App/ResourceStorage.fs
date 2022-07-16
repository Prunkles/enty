namespace enty.Web.App

open System
open Browser.Types
open Fable.Core
open Fable.SimpleHttp

type IResourceStorage =
    abstract Create: formData: FormData -> Async<Result<Uri, string>>

type ResourceStorage(baseUrl: string) =
    interface IResourceStorage with
        member _.Create(formData) = async {
            let rid = Guid.NewGuid()
            let! response =
                Http.request $"{baseUrl}{string rid}"
                |> Http.method POST
                |> Http.content (BodyContent.Form formData)
                |> Http.send
            if response.statusCode = 200 then
                return Ok (Uri($"{baseUrl}{string rid}"))
            else
                return Error response.responseText
        }

module ResourceStorageHardcodeImpl =
    let mutable resourceStorage: IResourceStorage = Unchecked.defaultof<_>
    let init () = async {
        let! response = Fetch.fetch "/storage-address" [] |> Async.AwaitPromise
        let! baseUrl = response.text() |> Async.AwaitPromise
        printfn $"[DBG] ResourceStorage base url: {baseUrl}"
        resourceStorage <- ResourceStorage(baseUrl)
    }
