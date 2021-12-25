namespace enty.WebApp.Utils


[<RequireQualifiedAccess>]
module Async =

    open Fable.Core

    let inline startSafe (work: Async<unit>) : unit =
        async {
            try
                do! work
            with ex ->
                JS.console.error(ex)
                return raise ex
//            match! Async.Catch(work) with
//            | Choice1Of2 () -> ()
//            | Choice2Of2 err -> JS.console.error(err)
        } |> Async.StartImmediate
