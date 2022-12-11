module enty.ResourceStorage.Server.Program

open System
open System.IO
open Microsoft.Net.Http.Headers
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Serilog
open Serilog.Templates
open Serilog.Templates.Themes
open pdewebq.Extensions.Serilog
open pdewebq.Extensions.Serilog.TemplateLogging

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
            policy.WithExposedHeaders(HeaderNames.ContentDisposition).AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod() |> ignore
        ) |> ignore
        app.UseEndpoints(fun endpoint ->
            endpoint.MapGiraffeEndpoints(HttpHandlers.endpoints)
        ) |> ignore

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


let createHostBuilder args =
    Host.CreateDefaultBuilder(args)
        .UseSerilog(Startup.configureSerilog)
        .ConfigureWebHostDefaults(fun webBuilder ->
            webBuilder
                .ConfigureServices(Startup.configureServices)
                .Configure(Startup.configureApp)
                .UseKestrel(fun ctx options ->
                    // https://github.com/dotnet/aspnetcore/issues/4765
                    let s = ctx.Configuration.GetSection("Kestrel:Limits:MaxRequestBodySize")
                    if s.Exists() then
                        options.Limits.MaxRequestBodySize <-
                            if s.Value = "null"
                            then Nullable()
                            else Nullable(s.Get<int64>())
                )
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
