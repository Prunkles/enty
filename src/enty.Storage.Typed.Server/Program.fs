open System
open Microsoft.Extensions.Hosting

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

open Giraffe

module Startup =

    open enty.Storage.Typed.Server

    
    let configureServices (ctx: WebHostBuilderContext) (services: IServiceCollection) : unit =
        services.AddGiraffe() |> ignore
    
    let configureApp (ctx: WebHostBuilderContext) (app: IApplicationBuilder) : unit =
        app.UseGiraffe(HttpHandlers.server)
        ()
    

let createHostBuilder args =
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webhost ->
            webhost
                .ConfigureServices(Startup.configureServices)
                .Configure(Startup.configureApp)
            |> ignore
        )

[<EntryPoint>]
let main argv =
    (createHostBuilder argv).Build().Start()
    0