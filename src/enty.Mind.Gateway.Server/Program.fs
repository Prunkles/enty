module enty.Mind.Gateway.Server.Program

open System
open System.Collections.Generic
open System.IO
open System.Text.RegularExpressions
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Serilog
open Serilog.Configuration
open Serilog.Core
open Serilog.Events
open pdewebq.Extensions.Serilog

open Serilog.Templates
open enty.Mind

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
        let loggerA = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("TestLoggerA")
        let loggerB = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("TestLoggerB")
        loggerA.LogInformation("A1 I")
        loggerA.LogDebug("A2 D")
        loggerB.LogInformation("B1 I")
        loggerB.LogDebug("B2 D")
        loggerB.LogError("B3 E")

        if ctx.HostingEnvironment.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore
        app.UseRouting() |> ignore
        app.UseEndpoints(fun endpoint ->
            endpoint.MapGiraffeEndpoints(Endpoints.endpoints)
        ) |> ignore
        app.UseGiraffe(Endpoints.notFoundHandler) |> ignore

let createWebHostBuilder args =
    Host.CreateDefaultBuilder(args)
        .UseSerilog(fun context services configuration ->
            let basePath = context.Configuration.["PLogging:BasePath"]
            let templates =
                context.Configuration.GetSection("PLogging:SourceContextTemplates").GetChildren()
                |> Seq.map ^fun c -> c.["SourceContext"], c.["Template"]
                |> Map.ofSeq
            configuration
                .MinimumLevel.Information()
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.WithMessageTemplate()
                .WriteTo.MapSourceContextAndDate(
                    templates
                    , fun sourceContext date ->
                        let dateS = date.ToString("yyyy'-'MM'-'dd")
                        Path.Combine(basePath, $"%s{dateS}_%s{sourceContext}.log")
                    , fun formatter path wt ->
                        match formatter with
                        | Some formatter -> wt.File(formatter, path) |> ignore
                        | None -> wt.File(path) |> ignore
                )
            |> ignore
        )
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
