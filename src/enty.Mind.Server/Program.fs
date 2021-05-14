﻿module enty.Mind.Server.Program

open System
open System.Threading.Tasks
open FSharp.Control
open FSharp.Control.Tasks.V2
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

open FluentMigrator.Runner

open Giraffe

open enty.Core
open enty.Mind
open enty.Mind.Server
open enty.Mind.WishParsing
open enty.Mind.Server.Database


//type Worker(mindService: IMindService, logger: ILogger<Worker>) =
//    inherit BackgroundService()
//
//    override this.ExecuteAsync(stoppingToken) =
//        let work = async {
//            let wishInput = """{
//                sys {
//                    file:name <!"test1.png">
//                } &
//                tags [ t00 & t02 | t11 ]
//            }"""
//            let wish =
//                Wish.parse wishInput
//                |> function Ok x -> x | Error err -> logger.LogError($"Failed parsing: {err}"); failwith err
//            logger.LogDebug($"Parsed wish: %A{wish}")
//            
//            let entityIds = mindService.Wish(wish)
//            let! entityIds = entityIds |> AsyncSeq.toArrayAsync
//            let eIdsStr = entityIds |> Seq.map (fun (EntityId x) -> string x) |> String.concat "\n"
//            logger.LogInformation($"Found entities:\n{eIdsStr}")
//        }
//        Async.StartAsTask(work, cancellationToken=stoppingToken) :> Task

module Startup =

    open Microsoft.AspNetCore.Builder
    open enty.Mind.Server.Database.Migrations
    
    let configureServices (host: WebHostBuilderContext) (services: IServiceCollection) : unit =
        let connectionString = host.Configuration.GetConnectionString("Default")

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
    
    let configureApp (host: WebHostBuilderContext) (app: IApplicationBuilder) : unit =
        app.UseCors("_AllowAll") |> ignore
        app.UseGiraffeErrorHandler(HttpHandler.errorHandler) |> ignore
        app.UseGiraffe(HttpHandler.app)
        
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
    printfn $"{TestDefine.runtime}: %A{TestDefine.result}"
    (createHostBuilder argv).Build().Run()
    0