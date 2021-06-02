module enty.ResourceStorage.Server.Program

open System

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting

module Startup =
    
    open Giraffe
    open enty.ResourceStorage
    open enty.ResourceStorage.FileSystem

    let configureServices (ctx: WebHostBuilderContext) (services: IServiceCollection) : unit =
        services.AddTransient<IResourceStorage>(fun sp ->
            let path = ctx.Configuration.["Storage:Path"]
            let nestingLevel = 1
            upcast FileSystemResourceStorage(path, nestingLevel)
        ) |> ignore
        services.AddGiraffe() |> ignore
    
    let configureApp (ctx: WebHostBuilderContext) (app: IApplicationBuilder) : unit =
        app.UseGiraffe(HttpHandlers.server)
    
    let configureLogging (ctx: WebHostBuilderContext) (logging: ILoggingBuilder) : unit =
        ()


let createHostBuilder args =
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(fun webhost ->
            webhost
                .ConfigureServices(Startup.configureServices)
                .Configure(Startup.configureApp)
                .ConfigureLogging(Startup.configureLogging)
            |> ignore
        )

[<EntryPoint>]
let main argv =
    (createHostBuilder argv).Build().Run()
    0
