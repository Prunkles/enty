namespace enty.Utils

[<RequireQualifiedAccess>]
module Result =

    let getOk = function
        | Ok x -> x
        | Error e -> invalidOp $"Result is Error %A{e}"

    let expectOk message = function
        | Ok x -> x
        | Error e -> invalidOp (message e)

    let allIsOk (results: Result<'a, 'e> list) : Result<'a list, 'e> =
        ((None: Result<'a list, 'e> option), results) ||> List.fold (fun s r ->
            match s with
            | None ->
                match r with
                | Ok x -> Ok [x]
                | Error e -> Error e
            | Some s ->
                match s with
                | Error e -> Error e
                | Ok s ->
                    match r with
                    | Ok x -> Ok [ yield! s; yield x ]
                    | Error e -> Error e
            |> Some
        )
        |> Option.get

    let allSeqIsOk (results: Result<'a, 'e> seq) : Result<'a seq, 'e> =
        ((None: Result<'a seq, 'e> option), results) ||> Seq.fold (fun s r ->
            match s with
            | None ->
                match r with
                | Ok x -> Ok (seq { x })
                | Error e -> Error e
            | Some s ->
                match s with
                | Error e -> Error e
                | Ok s ->
                    match r with
                    | Ok x -> Ok (seq { yield! s; yield x })
                    | Error e -> Error e
            |> Some
        )
        |> Option.get
