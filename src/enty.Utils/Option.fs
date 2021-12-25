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


type OptionBuilder() =
    member _.Bind(x, f) = Option.bind f x
    member _.Bind2(x1, x2, f: 'a * 'b -> 'c option) =
        match x1, x2 with
        | Some x1, Some x2 -> f (x1, x2)
        | _ -> None
    member _.Bind3(x1, x2, x3, f) =
        match x1, x2, x3 with
        | Some x1, Some x2, Some x3 -> f (x1, x2, x3)
        | _ -> None
    member _.Return(x) = Some x
    member _.BindReturn(x, f) = Option.map f x
    member _.Bind2Return(x1, x2, f: 'a * 'b -> 'c) =
        match x1, x2 with
        | Some x1, Some x2 -> Some (f (x1, x2))
        | _ -> None
    member _.ReturnFrom(x: _ option) = x
    member _.MergeSources(x1, x2) =
        match x1, x2 with
        | Some x1, Some x2 -> Some (x1, x2)
        | _ -> None
    member _.MergeSources3(x1, x2, x3) =
        match x1, x2, x3 with
        | Some x1, Some x2, Some x3 -> Some (x1, x2, x3)
        | _ -> None
    member _.Zero() = None

[<AutoOpen>]
module OptionBuilderImpl =
    let option = OptionBuilder()
