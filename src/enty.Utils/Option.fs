namespace enty.Utils

[<RequireQualifiedAccess>]
module Option =

    /// Choose first Some
    let choose opts = Seq.tryPick id opts

    let ofTryByref = function
        | true, v -> Some v
        | false, _ -> None

    let allIsSome (source: 'a option seq) : 'a seq option =
        (source, Some [])
        ||> Seq.foldBack ^fun x s ->
            match s, x with
            | Some s, Some x -> Some (x :: s)
            | _ -> None
        |> Option.map List.toSeq

    let allIsSomeList (source: 'a option list) : 'a list option =
        (source, Some [])
        ||> Seq.foldBack ^fun x s ->
            match s, x with
            | Some s, Some x -> Some (x :: s)
            | _ -> None
