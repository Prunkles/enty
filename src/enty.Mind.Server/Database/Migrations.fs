module enty.Mind.Server.Database.Migrations

open System
open FluentMigrator
open FluentMigrator.Runner
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection


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
//        .AddLogging(fun b -> b.AddFluentMigratorConsole() |> ignore)
        .AddFluentMigratorCore()
        .ConfigureRunner(fun b ->
            b.AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof<Init>.Assembly).For.Migrations()
            |> ignore
        )

let migrate (app: IApplicationBuilder) =
    use scope = app.ApplicationServices.CreateScope()
    let runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>()
    runner.MigrateUp()
