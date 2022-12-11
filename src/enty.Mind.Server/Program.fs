module enty.Mind.Server.Program

open System
open System.IO
open FSharp.Control
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Configuration

open LinqToDB.Configuration
open LinqToDB.AspNet
open LinqToDB.AspNet.Logging

open Giraffe
open Giraffe.EndpointRouting

open Serilog
open Serilog.Configuration
open Serilog.Templates
open Serilog.Templates.Themes
open pdewebq.Extensions.Serilog

open enty.Core
open enty.Mind.Server
open pdewebq.Extensions.Serilog.TemplateLogging


module Startup =

    open Khonsu.Coding.Json
    open Khonsu.Coding.Json.Net
    open enty.Mind.Server.Database.Migrations

    let configureServices (host: WebHostBuilderContext) (services: IServiceCollection) : unit =
        let connectionString = host.Configuration.GetConnectionString("Default")

        services.AddTransient<IJsonDecoding<JsonValue>, ThothJsonDecoding>() |> ignore
        services.AddTransient<IJsonEncoding<JsonValue>, ThothJsonEncoding>() |> ignore

        services.AddTransient<IMindService, DbMindService>() |> ignore

        // Migrations
        configureMigrations services connectionString |> ignore

        // Database
        LinqToDB.Common.Configuration.Linq.GenerateExpressionTest <- true
        services.AddLinqToDbContext<EntyDataConnection>(fun sp options ->
            options.UsePostgreSQL(connectionString) |> ignore
            options.UseDefaultLogging(sp) |> ignore
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

    let configureApp (context: WebHostBuilderContext) (app: IApplicationBuilder) : unit =
        if context.HostingEnvironment.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore
        app.UseRouting() |> ignore
        app.UseEndpoints(fun endpoint ->
            endpoint.MapGiraffeEndpoints(Endpoints.endpoints)
        ) |> ignore
        app.UseGiraffe(Endpoints.notFoundHandler) |> ignore

        migrate app

    let configureSerilog (context: HostBuilderContext) (services: IServiceProvider) (configuration: LoggerConfiguration) : unit =
        let consoleTemplates = LoggingTemplate.parseConfigurationMany (context.Configuration.GetRequiredSection("pdewebq:Logging:Console:Templates")) |> function Ok x -> x | Error e -> failwith $"{e}"
        let fileTemplates = LoggingTemplate.parseConfigurationMany (context.Configuration.GetRequiredSection("pdewebq:Logging:File:Templates")) |> function Ok x -> x | Error e -> failwith $"{e}"
        let filePathTemplate = context.Configuration.["pdewebq:Logging:File:Path"]
        configuration
            .MinimumLevel.Information()
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .WriteTo.MapTemplates(consoleTemplates, fun template _sourceContext wt ->
                let formatter = ExpressionTemplate(template.Template, theme=TemplateTheme.Code, applyThemeWhenOutputIsRedirected=true)
                wt.Console(formatter, ?restrictedToMinimumLevel=template.MinLevel) |> ignore
            )
            .WriteTo.MapTemplates(fileTemplates, fun template sourceContext wt ->
                wt.MapDateOnly(
                    (fun date wt ->
                        let formatter = ExpressionTemplate(template.Template)
                        let filePath = filePathTemplate.Replace("{SourceContext}", sourceContext).Replace("{Date}", date.ToString("yyyy'-'MM'-'dd"))
                        wt.File(
                            formatter, filePath,
                            ?restrictedToMinimumLevel=template.MinLevel
                        ) |> ignore
                    ),
                    sinkMapCountLimit=1
                ) |> ignore
            )
        |> ignore


[<CompiledName "CreateHostBuilder">]
let createHostBuilder args =
    Host.CreateDefaultBuilder(args)
        .UseSerilog(Startup.configureSerilog)
        .ConfigureWebHostDefaults(fun webBuilder ->
            webBuilder
                .Configure(Startup.configureApp)
                .ConfigureServices(Startup.configureServices)
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
