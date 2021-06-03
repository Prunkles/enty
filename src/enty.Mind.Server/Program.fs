module enty.Mind.Server.Program

open System
open System.Threading.Tasks
open FSharp.Control
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration

open LinqToDB
open LinqToDB.Configuration
open LinqToDB.AspNet
open LinqToDB.AspNet.Logging

open Giraffe

open enty.Core
open enty.Mind
open enty.Mind.Server


module Startup =

    open Khonsu.Coding.Json
    open Khonsu.Coding.Json.Net
    open Microsoft.AspNetCore.Builder
    open enty.Mind.Server.Database.Migrations

    let configureServices (host: WebHostBuilderContext) (services: IServiceCollection) : unit =
        let connectionString = host.Configuration.GetConnectionString("Default")

        services.AddTransient<IJsonDecoding<JsonValue>, ThothJsonDecoding>() |> ignore
        services.AddTransient<IJsonEncoding<JsonValue>, ThothJsonEncoding>() |> ignore

        services.AddTransient<IMind, DbMind>() |> ignore

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

    let configureApp (host: WebHostBuilderContext) (app: IApplicationBuilder) : unit =
        app.UseEndpoints(fun endpoints ->
            endpoints.MapGrpcService<GrpcServerMindService>() |> ignore
        ) |> ignore

        migrate app

    let configureLogging (builder: ILoggingBuilder) : unit =
        builder.AddConsole() |> ignore
//        builder.AddFluentMigratorConsole() |> ignore
        ()


[<CompiledName "CreateHostBuilder">]
let createHostBuilder args =
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webBuilder ->
            webBuilder
                .Configure(Startup.configureApp)
                .ConfigureServices(Startup.configureServices)
                .ConfigureLogging(Startup.configureLogging)
            |> ignore
        )

[<EntryPoint>]
let main argv =
    (createHostBuilder argv).Build().Run()
    0
