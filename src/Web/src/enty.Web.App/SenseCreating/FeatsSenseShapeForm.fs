module enty.Web.App.SenseCreating.FeatsSenseShapeForm

open System
open FsToolkit.ErrorHandling

open Feliz
open Feliz.UseElmish
open Feliz.MaterialUI
open Feliz.MaterialUI.Mui5
open Elmish

open enty.Core
open enty.Utils
open enty.Web.App.SenseFormatting
open enty.Web.App.SenseParsing
open enty.Web.App.Utils
open enty.Web.App.SenseShapes
open enty.Web.App.Components


type private FeatId = FeatId of int

[<RequireQualifiedAccess>]
module private FeatsSenseShapeForm =

    [<RequireQualifiedAccess>]
    type Msg =
        | FeatValueInputChanged of FeatId * featValueInput: string
        | FeatNameInputChanged of FeatId * featNameInput: string
        | CreateFeat
        | RemoveFeat of FeatId

    type FeatValue =
        { Input: string
          Sense: Result<Sense, SenseParseError> }

    type FeatName =
        { Input: string
          Value: Result<string, string> }

    type Feat =
        { Name: FeatName
          Value: FeatValue }

    type State =
        { Feats: Map<FeatId, Feat>
          LastFeatId: FeatId }

    let init (initialSense: Sense) : State * Cmd<Msg> =
        let feats =
            match FeatsSenseShape.parse initialSense with
            | None -> Map.empty
            | Some shape ->
                shape.Feats
                |> Map.toSeq
                |> Seq.indexed
                |> Seq.map ^fun (featId, (featName, featValue)) ->
                    FeatId featId, {
                        Name = {
                            Input = featName
                            Value = Ok featName
                        }
                        Value = {
                            Input = Sense.formatMultiline featValue
                            Sense = Ok featValue
                        }
                    }
                |> Map.ofSeq
        let state = {
            Feats = feats
            LastFeatId = FeatId (feats.Count - 1)
        }
        state, Cmd.none

    module FeatValue =
        let parse (input: string) : FeatValue =
            { Input = input
              Sense = Sense.parse input }

    let parseFeatName (featNameInput: string) : FeatName =
        let nameValue =
            if String.IsNullOrEmpty(featNameInput) then
                Error "Empty"
            else
                Ok featNameInput
        { Input = featNameInput; Value = nameValue }

    [<RequireQualifiedAccess>]
    module State =

        let nextFeatId (state: State) : State * FeatId =
            let featId = state.LastFeatId |> fun (FeatId i) -> FeatId (i + 1)
            let state = { state with LastFeatId = featId }
            state, featId

        let createFeat (featId: FeatId) (state: State) : State =
            let feat = { Name = parseFeatName ""; Value = { Input = ""; Sense = Ok (Sense.empty ()) } }
            let state = { state with Feats = state.Feats |> Map.add featId feat }
            state

        let removeFeat (featId: FeatId) (state: State) : State =
            let state = { state with Feats = state.Feats |> Map.remove featId }
            state

        let updateFeat (featId: FeatId) (updater: Feat -> Feat) (state: State) : State =
            let feat = state.Feats.[featId]
            let feats = state.Feats |> Map.add featId (updater feat)
            let state = { state with Feats = feats }
            state

    let update (msg: Msg) (state: State) : State * Cmd<Msg> =
        match msg with
        | Msg.CreateFeat ->
            let state, featId = state |> State.nextFeatId
            let state = state |> State.createFeat featId
            state, Cmd.none
        | Msg.RemoveFeat featId ->
            let state = state |> State.removeFeat featId
            state, Cmd.none
        | Msg.FeatValueInputChanged (featId, featValueInput) ->
            let state = state |> State.updateFeat featId (fun feat -> { feat with Value = FeatValue.parse featValueInput })
            state, Cmd.none
        | Msg.FeatNameInputChanged (featId, featNameInput) ->
            let state = state |> State.updateFeat featId (fun feat -> { feat with Name = parseFeatName featNameInput })
            state, Cmd.none

[<ReactComponent>]
let FeatsSenseShapeForm (initialSense: Sense) (onSenseChanged: Validation<Sense, string> -> unit) =
    let state, dispatch =
        React.useElmish(
            (fun () -> FeatsSenseShapeForm.init initialSense),
            FeatsSenseShapeForm.update,
            ()
        )

    // ----

    React.useEffect(fun () ->
        onSenseChanged ^ validation {
            let! feats =
                state.Feats
                |> Map.values
                |> Seq.toList
                |> List.traverseResultA ^fun feat -> validation {
                    let! featName = feat.Name.Value |> Result.mapError (fun e -> $"Feat name: {e}")
                    and! featValueSense = feat.Value.Sense |> Result.mapError (fun e -> $"Feat value: %A{e}")
                    return featName, featValueSense
                }
                |> Result.mapError (List.collect id)
            return senseMap {
                "feats", senseMap {
                    yield! feats
                }
            }
        }
    , [| state.Feats :> obj |])

    // ----

    let handleFeatValueInputChanged (featId: FeatId) (featValueInput: string) =
        dispatch (FeatsSenseShapeForm.Msg.FeatValueInputChanged (featId, featValueInput))

    let handleFeatNameChanged (featId: FeatId) (featName: string) =
        dispatch (FeatsSenseShapeForm.Msg.FeatNameInputChanged (featId, featName))

    Mui.stack @+ [
        stack.direction.column
        stack.spacing 2
    ] <| [
        for featId, feat in state.Feats |> Map.toSeq do
            Mui.stack @+ [
                prop.key (featId |> function FeatId i -> i)
                stack.direction.row
                stack.spacing 1
            ] <| [
                Mui.textField [
                    textField.label "Name"
                    textField.value feat.Name.Input
                    textField.onChange (handleFeatNameChanged featId)
                    match feat.Name.Value with
                    | Ok _ -> ()
                    | Error error ->
                        textField.error true
                        textField.helperText error
                ]
                Mui.textField [
                    textField.label "Value"
                    textField.multiline true
                    textField.fullWidth true
                    textField.value feat.Value.Input
                    textField.onChange (handleFeatValueInputChanged featId)
                    match feat.Value.Sense with
                    | Ok _ -> ()
                    | Error error ->
                        textField.error true
                        textField.helperText (SenseParseErrorComp error)
                ]
                Mui.stack @+ [ stack.direction.column; prop.style [ style.justifyContent.center ] ] <| [
                    Mui.iconButton [
                        prop.onClick (fun _e -> dispatch (FeatsSenseShapeForm.Msg.RemoveFeat featId))
                        iconButton.children (Mui.icon "delete")
                    ]
                ]
            ]
        Mui.stack @+ [ stack.direction.column; prop.style [ style.justifyContent.center ] ] <| [
            Mui.button [
                button.variant.contained
                prop.onClick (fun _e -> dispatch FeatsSenseShapeForm.Msg.CreateFeat)
                button.startIcon (Mui.icon "add_circle")
                button.children "Add"
            ]
        ]
    ]
