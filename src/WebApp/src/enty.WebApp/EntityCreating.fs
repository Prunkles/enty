module enty.WebApp.EntityCreating

open System
open System.Collections.Generic
open System.Text
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open Feliz
open Feliz.MaterialUI
open Fable.SimpleHttp
open enty.Core
open enty.Utils
open enty.WebApp.Utils
open MindApiImpl


module Seq =

    let trySequentiallyCompose (funs: ('a -> 'a) seq) : ('a -> 'a) option =
        Seq.fold (fun s x -> Some ^ match s with None -> x | Some f -> f >> x) None funs

module Result =

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

    // let allIsOk (results: Result<'a, 'e> seq) : Result<'a seq, 'e> =
    //     ((None: Result<'a seq, 'e> option), results) ||> Seq.fold (fun s r ->
    //         match s with
    //         | None ->
    //             match r with
    //             | Ok x -> Ok (seq { x })
    //             | Error e -> Error e
    //         | Some s ->
    //             match s with
    //             | Error e -> Error e
    //             | Ok s ->
    //                 match r with
    //                 | Ok x -> Ok (seq { yield! s; yield x })
    //                 | Error e -> Error e
    //         |> Some
    //     )
    //     |> Option.get

//[<RequireQualifiedAccess>]
//module Sense =
//
//    let display (sense: Sense) : string =
//        let sb = StringBuilder()
//        let rec printSense (sb: StringBuilder) sense =
//            match sense with
//            | Sense.Value v -> sb.Append('"').Append(v).Append('"') |> ignore
//            | Sense.List l ->
//                sb.Append('[') |> ignore
//                for e in l do
//                    printSense sb e
//                    sb.Append(' ') |> ignore
//                sb.Append(']') |> ignore
//            | Sense.Map m ->
//                sb.Append('{') |> ignore
//                for KeyValue (k, v) in m do
//                    sb.Append(k).Append(' ') |> ignore
//                    printSense sb v
//                    sb.Append(' ') |> ignore
//                sb.Append('}') |> ignore
//        printSense sb sense
//        sb.ToString()



//[<ReactComponent>]
//let SenseInput () =
//    let input, setInput = React.useState("")
//    Mui.textareaAutosize [
//        prop.value input
//        prop.onChange setInput
//    ]
//
//let createResourceUrl (ridG: Guid) : string =
//    $"/storage/{ridG}"
//
//let writeResource formData = async {
//    let ridG = Guid.NewGuid()
//    let url = createResourceUrl ridG
//    printfn "Uri: %A" url
//    let! response =
//        Http.request url
//        |> Http.method POST
//        |> Http.content (BodyContent.Form formData)
//        |> Http.send
//    if response.statusCode = 200 then
//        return Ok url
//    else
//        return Error ()
//}


type TraitForm = (Result<Sense, string> -> unit) -> ReactElement

[<ReactComponent>]
let ImageTraitForm (onSenseChanged: Result<Sense, string> -> unit) =
    let changeUrl (url: string) =
        // TODO: Validate URL
        match Uri.TryCreate(url, UriKind.Absolute) with
        | false, _ ->
            onSenseChanged (Error "Invalid URL")
        | true, uri ->
            let newSense = senseMap {
                "image", senseMap {
                    "url", url
                }
            }
            onSenseChanged (Ok newSense)

    Html.div [
        Mui.textField [
            textField.required true
            textField.placeholder "URL"
            textField.onChange changeUrl
        ]
    ]


[<ReactComponent>]
let EntityCreateForm (onSubmit: Sense -> unit) (forms: TraitForm list) : ReactElement =
    let forms: (TraitForm * Guid) list =
        React.useMemo(
            fun () ->
                printfn "Gen guids"
                forms |> Seq.map (fun form -> form, Guid.NewGuid()) |> Seq.toList
            , [| forms |]
        )

    let (formResults: Map<Guid, Result<Sense, string> option>), setFormResults =
        React.useState ^fun () ->
            forms |> Seq.map (fun (_, g) -> g, None) |> Map.ofSeq

    let onFormSense (formId: Guid) (senseOpt: Result<Sense, string>) : unit =
        let newFormResults = formResults |> Map.add formId (Some senseOpt)
        printfn $"newFormResults: %A{newFormResults}"
        setFormResults newFormResults

    Mui.paper [
        Mui.paper [
//            paper.elevation 1
            paper.children [
                for form, formId in forms do
                    form (onFormSense formId)
            ]
        ]
        Html.button [
            prop.text "Create"
            let formResults =
                formResults |> Map.toSeq |> Seq.map snd |> Seq.toList
                |> Option.allIsSomeList
                |> Option.map Result.allIsOk
            match formResults with
            | Some (Ok formResults) ->
                prop.disabled false
                prop.onClick ^fun _ ->
                    // TODO: Resolve empty results and unmergable senses
                    let mergedSense = formResults |> Seq.reduce Sense.merge
                    onSubmit mergedSense
            | _ ->
                prop.disabled true
        ]
    ]
