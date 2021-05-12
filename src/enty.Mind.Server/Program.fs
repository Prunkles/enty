module enty.Mind.Server.Program

open System
open System.Threading.Tasks
open FSharp.Control
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration

open LinqToDB
open LinqToDB.Configuration
open LinqToDB.AspNet
open LinqToDB.AspNet.Logging

open enty.Core
open enty.Mind
open enty.Mind.Server
open enty.Mind.Server.Database


type Worker(mindService: IMindService) =
    inherit BackgroundService()

    override this.ExecuteAsync(stoppingToken) =
        let work = async {
            let wish =
                WishAst.Or (
                    WishAst.Not <| WishAst.Equals (["a"], "1"),
                    WishAst.Equals (["a"], "2")
                )
//                WishAst.Not (WishAst.Equals (["a"], "2"))
//                WishAst.Equals (["b"], "1")
            
            let entityIds = mindService.Wish(wish)
            do!
                entityIds
                |> AsyncSeq.iter (printfn "%A")
        }
        Async.StartAsTask(work, cancellationToken=stoppingToken) :> Task

module Startup =

    open enty.Mind.Server.Database.Migrations
    
    let configureServices (host: HostBuilderContext) (services: IServiceCollection) =
        let connectionString = host.Configuration.GetConnectionString("Default")

        services.AddTransient<IMindService, DbMindService>() |> ignore
        
        // Migrations
        configureMigrations services connectionString |> ignore
        services.AddHostedService<MigratorService>() |> ignore
        
        // Database
        LinqToDB.Common.Configuration.Linq.GenerateExpressionTest <- true
        services.AddLinqToDbContext<EntyDataConnection>(fun sp options ->
            options.UsePostgreSQL(connectionString) |> ignore
            options.UseDefaultLogging(sp) |> ignore
        ) |> ignore
        
        services.AddHostedService<Worker>() |> ignore
        ()

    let configureLogging (builder: ILoggingBuilder) =
        builder


[<CompiledName "CreateHostBuilder">]
let createHostBuilder args =
    Host.CreateDefaultBuilder(args)
        .ConfigureServices(Startup.configureServices)
        .ConfigureLogging(Startup.configureLogging >> ignore)

[<EntryPoint>]
let main argv =
    (createHostBuilder argv).Build().Run()
    0
