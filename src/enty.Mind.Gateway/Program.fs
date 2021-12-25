module enty.Mind.Gateway.Program

open enty.Mind
open System
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting

module Startup =

    open System.Net.Http
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
            opt.ChannelOptionsActions.Add(fun o ->
                let httpHandler = new HttpClientHandler()
                httpHandler.ServerCertificateCustomValidationCallback <- HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                o.HttpHandler <- httpHandler
            )
        ) |> ignore
        services.AddTransient<IMindService, GrpcClientMindService>() |> ignore
        services.AddGiraffe() |> ignore

    let configureApp (ctx: WebHostBuilderContext) (app: IApplicationBuilder) : unit =
        app.UseRouting() |> ignore
        app.UseEndpoints(fun endpoint ->
            endpoint.MapGiraffeEndpoints(Endpoints.endpoints)
        ) |> ignore
        app.UseGiraffe(Endpoints.notFoundHandler) |> ignore


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
