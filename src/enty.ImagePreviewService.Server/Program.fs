module enty.ImagePreviewService.Server.Program

open System
open System.IO
open System.Net.Http
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open SixLabors.ImageSharp.Web
open SixLabors.ImageSharp.Web.Providers
open SixLabors.ImageSharp.Web.Resolvers
open enty.Utils

type HttpResponseImageResolver(response: HttpResponseMessage) =
    interface IImageResolver with
        member this.GetMetaDataAsync() = task {
            let metadata =
                option {
                    let! lastModified = response.Content.Headers.LastModified |> Option.ofNullable
                    and! contentLength = response.Content.Headers.ContentLength |> Option.ofNullable

                    return ImageMetadata(lastModified.DateTime, contentLength)
                }
                |> Option.defaultWith (fun () -> ImageMetadata())
            return metadata
        }
        member this.OpenReadAsync() = task {
            return! response.Content.ReadAsStreamAsync()
        }

type UrlImageProvider(logger: ILogger<UrlImageProvider>, httpClientFactory: IHttpClientFactory) =
    let mutable match' = Func<_, _>(fun _ -> true)
    interface IImageProvider with
        member this.ProcessingBehavior = ProcessingBehavior.CommandOnly
        member this.Match with get() = match' and set v = match' <- v
        member this.IsValidRequest(context) =
            context.Request.Query.ContainsKey("url")
        member this.GetAsync(context) = task {
            let url = context.Request.Query.["url"].[0]
            let client = httpClientFactory.CreateClient()
            let! response = client.GetAsync(url)
            return HttpResponseImageResolver(response) :> IImageResolver
        }


module Startup =

    open Microsoft.AspNetCore.Builder
    open Microsoft.Extensions.DependencyInjection
    open SixLabors.ImageSharp.Web.DependencyInjection

    let configureServices (ctx: WebHostBuilderContext) (services: IServiceCollection) : unit =
        services.AddHttpClient() |> ignore
        services.AddImageSharp()
            .RemoveProvider<PhysicalFileSystemProvider>()
            .AddProvider<UrlImageProvider>()
        |> ignore

    let configureApp (ctx: WebHostBuilderContext) (app: IApplicationBuilder) : unit =
        app.UseImageSharp() |> ignore
        app.UseRouting() |> ignore

let createWebHostBuilder args =
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webBuilder ->
            webBuilder
                .ConfigureServices(Startup.configureServices)
                .Configure(Startup.configureApp)
            |> ignore
        )

[<EntryPoint>]
let main argv =
    (createWebHostBuilder argv).Build().Run()
    0
