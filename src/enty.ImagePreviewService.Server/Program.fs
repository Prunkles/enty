module enty.ImagePreviewService.Server.Program

open System
open System.IO
open System.Net.Http
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Metadata
open SixLabors.ImageSharp.Web
open SixLabors.ImageSharp.Web.Providers
open FSharp.Control.Tasks
open SixLabors.ImageSharp.Web.Resolvers

type HttpResponseImageResolver(response: HttpResponseMessage) =
    interface IImageResolver with
        member this.GetMetaDataAsync() = task {
            let metadata =
                option {
                    let! lastModified = response.Content.Headers.LastModified |> Option.ofNullable
                    let! contentLength = response.Content.Headers.ContentLength |> Option.ofNullable
                    return ImageMetadata(lastModified.DateTime, contentLength)
                }
                |> Option.defaultWith (fun () -> ImageMetadata())
            return metadata
        }
        member this.OpenReadAsync() = task {
            return! response.Content.ReadAsStreamAsync()
        }

type UrlImageProvider(logger: ILogger<UrlImageProvider>, httpClientFactory: IHttpClientFactory) =
    interface IImageProvider with
        member this.ProcessingBehavior = ProcessingBehavior.CommandOnly
        member this.Match with get() = Func<_, _>(fun _ -> true) and set _ = ()
        member this.IsValidRequest(context) =
            logger.LogDebug($"Path: {context.Request.Path}")
            true
        member this.GetAsync(context) = task {
            let url = context.Request.Query.["url"].[0]
            let client = httpClientFactory.CreateClient()
            let! response = client.GetAsync(url)
            return upcast HttpResponseImageResolver(response)
        }

module Endpoints =
    
    open Giraffe
    
    let endpoint: HttpHandler =
        choose [
            RequestErrors.notFound (text "Not found")
        ]

module Startup =

    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Hosting
    open Microsoft.Extensions.DependencyInjection
    open SixLabors.ImageSharp.Web.DependencyInjection
    open Giraffe
    
    let configureServices (ctx: WebHostBuilderContext) (services: IServiceCollection) : unit =
        services.AddHttpClient() |> ignore
        services.AddImageSharp()
            .RemoveProvider<PhysicalFileSystemProvider>()
            .AddProvider<UrlImageProvider>()
        |> ignore
    
    let configureApp (ctx: WebHostBuilderContext) (app: IApplicationBuilder) : unit =
        app.UseImageSharp() |> ignore
        app.UseGiraffe(Endpoints.endpoint)

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
