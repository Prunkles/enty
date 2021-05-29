module enty.Mind.Gateway.Program

open enty.Mind
open System
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting

module Startup =

    open Microsoft.AspNetCore.Builder
    open Microsoft.Extensions.DependencyInjection
    
    open Giraffe
    open Giraffe.EndpointRouting
    open enty.Mind.Client
    open enty.Mind.Client.GrpcMindServiceImpl
    
    let configureServices (ctx: WebHostBuilderContext) (services: IServiceCollection) : unit =
        let mindAddress = ctx.Configuration.["Mind:Address"]
        services.AddGrpcClient<Proto.MindService.MindServiceClient>(fun opt ->
            opt.Address <- Uri(mindAddress)
        ) |> ignore
        services.AddTransient<IMindService, GrpcClientMindService>() |> ignore
        services.AddGiraffe() |> ignore
    
    let configureApp (ctx: WebHostBuilderContext) (app: IApplicationBuilder) : unit =
        app.UseRouting()
            .UseEndpoints(fun endpoint ->
                endpoint.MapGiraffeEndpoints(Endpoints.endpoints)
            )
            .UseGiraffe(Endpoints.notFoundHandler)
        ()


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
