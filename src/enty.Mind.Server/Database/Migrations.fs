module enty.Mind.Server.Database.Migrations

open System
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open FluentMigrator
open FluentMigrator.Postgres
open FluentMigrator.Runner
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

[<Migration(20210508172000L)>]
type Init() =
    inherit Migration()

    override this.Up() =
        this.Create.Table("Entities")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("Sense").AsCustom("jsonb").NotNullable()
        |> ignore
    
    override this.Down() =
        this.Delete.Table("Entities")
        |> ignore

let configureMigrations (services: IServiceCollection) (connectionString: string) =
    services
        .AddFluentMigratorCore()
        .ConfigureRunner(fun b ->
            b.AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof<Init>.Assembly).For.Migrations()
            |> ignore
        )

//let updateDatabase (services: IServiceProvider) =
//    let runner = services.GetRequiredService<IMigrationRunner>()
//    runner.MigrateUp()

type MigratorService(runner: IMigrationRunner) =
    inherit BackgroundService()

    override this.ExecuteAsync(stoppingToken) =
        task {
            runner.MigrateUp()
        } :> Task
