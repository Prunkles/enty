open System
open Microsoft.Extensions.Hosting

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

open Giraffe

module Startup =

    open System.Net.Http
    open enty.Mind.Client
    open enty.Mind.Server.Api
    open enty.Storage.Typed.Server

    
    let configureServices (ctx: WebHostBuilderContext) (services: IServiceCollection) : unit =
        let storageUrl = ctx.Configuration.["Storage:Url"]
        let mindUrl = ctx.Configuration.["Mind:Url"]
        services.AddHttpClient<HttpClient, HttpClient>("storage", fun client ->
            client.BaseAddress <- Uri(storageUrl)
        ) |> ignore
        services.AddHttpClient<HttpClient, HttpClient>("mind", fun client ->
            client.BaseAddress <- Uri(mindUrl)
        ) |> ignore
        services.AddTransient<IMindService>(fun sp ->
            let httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("mind")
            let mindApi = ClientMindApi(httpClient)
            upcast ApiMindService(mindApi, Sense.toJToken >> SenseDto, (fun (SenseDto j) -> Sense.ofJToken j))
        ) |> ignore
        services.AddGiraffe() |> ignore
    
    let configureApp (ctx: WebHostBuilderContext) (app: IApplicationBuilder) : unit =
        app.UseGiraffe(HttpHandlers.server)
        ()
    

let createHostBuilder args =
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webhost ->
            webhost
                .ConfigureServices(Startup.configureServices)
                .Configure(Startup.configureApp)
            |> ignore
        )

[<EntryPoint>]
let main argv =
    (createHostBuilder argv).Build().Run()
    0
