namespace enty.Web.App.Utils


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

open Elmish

[<RequireQualifiedAccess>]
module Cmd =
    let ofAsyncDispatch (work: Dispatch<'msg> -> Async<unit>) : Cmd<'msg> =
        Cmd.ofSub ^fun dispatch ->
            work dispatch
            |> Async.startSafe


[<AutoOpen>]
module FelizExtensions =

    open Feliz

    let withProps elementFunction props (children: #seq<ReactElement>) =
        prop.children (children :> ReactElement seq) :: props |> elementFunction

    let inline ( @+ ) elementFunction props =
        fun children -> withProps elementFunction props children

    type React =
        static member useAsync(work: Async<'a>, ?deps: obj array): 'a option =
            let deps = defaultArg deps [| |]
            let value, setValue = React.useState(None)
            React.useEffect(fun () ->
                async {
                    let! x' = work
                    setValue (Some x')
                }
                |> Async.startSafe
            , deps)
            value
