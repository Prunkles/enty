module enty.ResourceStorage.Server.Program

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Serilog
open pdewebq.Extensions.Serilog

module Startup =

    open Giraffe
    open Giraffe.EndpointRouting
    open enty.ResourceStorage
    open enty.ResourceStorage.FileSystem

    let configureServices (ctx: WebHostBuilderContext) (services: IServiceCollection) : unit =
        // services.AddHttpLogging(fun logging ->
        //     logging.RequestHeaders.Add(HeaderNames.ContentDisposition) |> ignore
        // ) |> ignore
        services.AddTransient<IResourceStorage>(fun sp ->
            let logger = sp.GetRequiredService<ILogger<FileSystemResourceStorage>>()
            let path = ctx.Configuration.["Storage:Path"]
            let nestingLevel = 1
            upcast FileSystemResourceStorage(logger, path, nestingLevel)
        ) |> ignore
        services.AddCors() |> ignore
        services.AddGiraffe() |> ignore

    let configureApp (ctx: WebHostBuilderContext) (app: IApplicationBuilder) : unit =
        // app.UseHttpLogging() |> ignore
        app.UseRouting() |> ignore
        app.UseCors(fun policy ->
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod() |> ignore
        ) |> ignore
        app.UseEndpoints(fun endpoint ->
            endpoint.MapGiraffeEndpoints(HttpHandlers.endpoints)
        ) |> ignore

    let configureSerilog (context: HostBuilderContext) (services: IServiceProvider) (configuration: LoggerConfiguration) : unit =
        let basePath = context.Configuration.["PLogging:BasePath"]
        let templates =
            context.Configuration.GetSection("PLogging:SourceContextTemplates").GetChildren()
            |> Seq.map ^fun c -> c.["SourceContext"], c.["Template"]
            |> Map.ofSeq
        configuration
            .MinimumLevel.Information()
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
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


let createHostBuilder args =
    Host.CreateDefaultBuilder(args)
        .UseSerilog(Startup.configureSerilog)
        .ConfigureWebHostDefaults(fun webBuilder ->
            webBuilder
                .ConfigureServices(Startup.configureServices)
                .Configure(Startup.configureApp)
            |> ignore
        )

[<EntryPoint>]
let main argv =
    Log.Logger <- LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger()
    try
        try
            (createHostBuilder argv).Build().Run()
            0
        with ex ->
            Log.Fatal(ex, "An unhandled exception occured during bootstrapping")
            1
    finally
        Log.CloseAndFlush()
