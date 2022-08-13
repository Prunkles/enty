module enty.Web.App.SenseCreating.UserShapeForm

open System
open FsToolkit.ErrorHandling

open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5

open enty.Core
open enty.Utils
open enty.Web.App.Utils
open enty.Web.App.SenseShapes

let boxx = Operators.box

[<ReactComponent>]
let UserSenseShapeForm (initialSense: Sense) (onSenseChanged: Validation<Sense, string> -> unit) =

    let usernameInput, setUserInput =
        React.useState ^fun () ->
            match UserSenseShape.parse initialSense with
            | None -> ""
            | Some x -> x.UserName
    let ratingInput, setRatingInput =
        React.useState ^fun () ->
            match UserSenseShape.parse initialSense with
            | None -> ""
            | Some shape ->
                match shape.Rating with
                | None -> ""
                | Some r -> string r

    // ----

    let username = React.useMemo(fun () ->
        if usernameInput = String.Empty then
            None
        else
            Some usernameInput
    , [| boxx usernameInput |])

    let rating: Validation<float option, string> = React.useMemo(fun () ->
        printfn $"ratingInput: {ratingInput}"
        if ratingInput = String.Empty then
            Ok None
        else
            match Double.TryParse(ratingInput) |> Option.ofTryByref with
            | None ->
                Validation.error "Invalid number"
            | Some rating ->
                Ok (Some rating)
    , [| boxx ratingInput |])

    // ----

    React.useEffect(fun () ->
        printfn $"user: %A{username}"
        printfn $"rating: %A{rating}"
        onSenseChanged ^ validation {
            let! username = username |> function Some u -> Validation.ok u | None -> Validation.error "No username"
            and! rating = rating
            return senseMap {
                "user", senseMap {
                    "username", username
                    match rating with
                    | Some rating -> "rating", string rating
                    | None -> ()
                }
            }
        }
    , [| boxx username; boxx rating |])

    // ----

    let handleUserInputChanged (input: string) =
        setUserInput input

    let handleRatingInputChanged (input: string) =
        setRatingInput input

    Mui.stack @+ [
        stack.direction.column
        stack.spacing 1
    ] <| [
        Mui.textField [
            textField.label "Username"
            textField.onChange handleUserInputChanged
            textField.value usernameInput
            textField.required true
            match username with
            | None ->
                textField.error true
                textField.helperText "Username is required"
            | _ -> ()
        ]
        Mui.textField [
            textField.required false
            textField.label "Rating (optional)"
            textField.value ratingInput
            textField.onChange handleRatingInputChanged
            match rating with
            | Ok _ -> ()
            | Error errors ->
                textField.error true
                textField.helperText errors
        ]
    ]
