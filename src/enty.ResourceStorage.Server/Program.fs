module enty.ResourceStorage.Server.Program

open System

open System.Net.Http.Headers
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Net.Http.Headers

module Startup =

    open Giraffe
    open Giraffe.EndpointRouting
    open enty.ResourceStorage
    open enty.ResourceStorage.FileSystem

    let configureServices (ctx: WebHostBuilderContext) (services: IServiceCollection) : unit =
        services.AddHttpLogging(fun logging ->
            logging.RequestHeaders.Add(HeaderNames.ContentDisposition) |> ignore
        ) |> ignore
        services.AddTransient<IResourceStorage>(fun sp ->
            let logger = sp.GetRequiredService<ILogger<FileSystemResourceStorage>>()
            let path = ctx.Configuration.["Storage:Path"]
            let nestingLevel = 1
            upcast FileSystemResourceStorage(logger, path, nestingLevel)
        ) |> ignore
        services.AddCors(fun cors ->
            cors.AddPolicy("_AllowAll", fun builder ->
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                |> ignore
            )
        ) |> ignore
        services.AddGiraffe() |> ignore

    let configureApp (ctx: WebHostBuilderContext) (app: IApplicationBuilder) : unit =
        app.UseHttpLogging() |> ignore
        app.UseRouting() |> ignore
        app.UseCors("_AllowAll") |> ignore
        app.UseEndpoints(fun endpoint ->
                endpoint.MapGiraffeEndpoints(HttpHandlers.endpoints)
            )
        |> ignore

    let configureLogging (ctx: WebHostBuilderContext) (logging: ILoggingBuilder) : unit =
        ()


let createHostBuilder args =
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(fun webBuilder ->
            webBuilder
                .ConfigureServices(Startup.configureServices)
                .Configure(Startup.configureApp)
                .ConfigureLogging(Startup.configureLogging)
            |> ignore
        )

[<EntryPoint>]
let main argv =
    (createHostBuilder argv).Build().Run()
    0
