module enty.Mind.Server.Program

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
open pdewebq.Extensions.Serilog

open enty.Core
open enty.Mind.Server


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


[<CompiledName "CreateHostBuilder">]
let createHostBuilder args =
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
