module enty.Storage.FileSystem.Cli.ClaParsing


type ClaParser<'a, 'e> = string list -> string list * Result<'a, 'e>

module ClaParser =
    
    let run args (parser: ClaParser<_, _>) =
        let _, r = parser args
        r
    
    let retn x : ClaParser<_, _> = fun args -> args, Ok x
    
    let bind (binder: 'a -> ClaParser<'b, 'e>) (parser: ClaParser<'a, 'e>) : ClaParser<'b, 'e> =
        fun args ->
            let r = parser args
            match r with
            | args, Ok x ->
                let parser = binder x
                parser args
            | args, Error e -> args, Error e
    
    let map f x = bind (f >> retn) x
    
//    let withArgs (argsMapping: string list -> string list) (parser: ClaParser<'a, 'e>) : ClaParser<'a, 'e> =
//        fun args ->
//            let args' = argsMapping args
//            parser args'


type ClaParserBuilder() =
    
//    member _.Bind(x: string list -> (string * string list) option, f: string option -> ClaParser<_, _>) : ClaParser<_, _> =
//        fun args ->
//            match x args with
//            | Some (head, tail) ->
//                let r = f (Some head)
//                r tail
//            | None -> (f None) args
    
    member _.Bind(x, f) = ClaParser.bind f x
    
    member _.Return(x): ClaParser<_, _> = ClaParser.retn x
    
    member _.ReturnFrom(x: Result<_, _>): ClaParser<_, _> = fun args -> args, x


let claParser = ClaParserBuilder()


module Cla =
    
    let getArgs () : ClaParser<_, _> = fun args -> args, Ok args
    
    let shift () : ClaParser<_, 'e> =
        fun args ->
            match args with
            | head :: tail -> tail, Ok (Some head)
            | _ -> args, Ok None
    
//    let choose (fs: ClaParser<'a, 'e> seq) : ClaParser<'a, unit> =
//        fun args ->
//            let r =
//                fs |> Seq.tryPick (fun parser ->
//                    let r = parser args
//                    match r with
//                    | Ok x -> Some x
//                    | Error _ -> None
//                )
//            match r with
//            | Some x -> Ok x
//            | None -> Error ()

module private Playground =
    
    let parseTestCla args =
        let parser = claParser {
            let! arg = Cla.shift ()
            match arg with
            | Some "write" ->
                return 1
            | Some "read" ->
                return 2
            | Some "delete" ->
                return 3
            | Some "files" ->
                return 4
            | _ ->
                return! Error "Invalid arg"
        }
        parser args
    