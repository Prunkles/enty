namespace FApi.Giraffe

open Giraffe
open FSharp.Control.Tasks.V2

module Http =
    
    module Json =

        open FApi
        open FApi.JsonScheming
        open Microsoft.AspNetCore.Http
        
        let get (endpoint: HttpGetEndpointDeclaration) (handler: HttpContext -> 'Request -> Async<'Response>) : HttpHandler =
            let httpHandler: HttpHandler = fun next ctx -> task {
                let handler = handler ctx
                
                return ()
            }
            GET >=> httpHandler
