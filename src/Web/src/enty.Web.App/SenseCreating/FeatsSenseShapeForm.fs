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

    type Feat =
        { Name: Result<string, string>
          Value: Result<SenseValue, SenseParseError> }

    // TODO?: Remove it?
    type FeatField =
        { NameField: string
          ValueField: string }

    type FeatFeatField =
        { Feat: Feat
          Field: FeatField }

    type State =
        { Feats: Map<FeatId, FeatFeatField>
          FeatNameDuplicates: Set<string>
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
                    let featId = FeatId featId
                    let field = { NameField = featName; ValueField = SenseValue.formatMultiline featValue }
                    let feat = { Name = Ok featName; Value = Ok featValue }
                    featId, { Feat = feat; Field = field }
                |> Map.ofSeq
        let state = {
            Feats = feats
            FeatNameDuplicates = Set.empty
            LastFeatId = FeatId (feats.Count - 1)
        }
        state, Cmd.none

    module FeatValue =
        let parse (input: string) : Result<SenseValue, SenseParseError> =
            SenseValue.parse input

    module FeatName =
        let parse (featNameInput: string) : Result<string, string> =
            if String.IsNullOrEmpty(featNameInput) then
                Error "Empty"
            else
                Ok featNameInput

    [<RequireQualifiedAccess>]
    module State =

        let private validateFeatNameDuplicates (state: State) : State =
            let featNameDuplicates =
                state.Feats
                |> Map.toSeq
                |> Seq.choose ^fun (featId, feat) ->
                    match feat.Feat.Name with
                    | Ok nameValue -> Some (featId, nameValue)
                    | _ -> None
                |> Seq.countBy (fun (_id, nameValue) -> nameValue)
                |> Seq.choose (fun (name, c) -> if c >= 2 then Some name else None)
                |> Set.ofSeq
            { state with FeatNameDuplicates = featNameDuplicates }

        let nextFeatId (state: State) : State * FeatId =
            let featId = state.LastFeatId |> fun (FeatId i) -> FeatId (i + 1)
            let state = { state with LastFeatId = featId }
            state, featId

        let createFeat (featId: FeatId) (state: State) : State =
            let feat = { Name = FeatName.parse ""; Value = Ok (SenseValue.atom "") }
            let field = { NameField = ""; ValueField = "" }
            let state = { state with Feats = state.Feats |> Map.add featId { Feat = feat; Field = field } }
            state

        let removeFeat (featId: FeatId) (state: State) : State =
            let state = { state with Feats = state.Feats |> Map.remove featId }
            let state = state |> validateFeatNameDuplicates
            state

        let updateFeat (featId: FeatId) (updater: FeatFeatField -> FeatFeatField) (state: State) : State =
            let feats =
                let feat = state.Feats.[featId]
                state.Feats |> Map.add featId (updater feat)
            let state = { state with Feats = feats }
            let state = state |> validateFeatNameDuplicates
            state

    let setFeatName (feat: FeatFeatField) (featName: string) : FeatFeatField =
        { feat with
            Feat = { feat.Feat with Name = FeatName.parse featName }
            Field = { feat.Field with NameField = featName }
        }

    let setFeatValue (feat: FeatFeatField) (featValue: string) : FeatFeatField =
        { feat with
            Feat = { feat.Feat with Value = FeatValue.parse featValue }
            Field = { feat.Field with ValueField = featValue }
        }

    let getCheckedFeatName (state: State) (feat: Feat) : Result<string, string> =
        match feat.Name with
        | Ok featName ->
            if state.FeatNameDuplicates |> Set.contains featName then
                Error $"Feat {featName} already exists"
            else
                Ok featName
        | Error e -> Error e

    let update
            (onSenseChanged: Validation<Sense, string> -> unit)
            (msg: Msg) (state: State)
            : State * Cmd<Msg> =
        let state, cmd =
            match msg with
            | Msg.CreateFeat ->
                let state, featId = state |> State.nextFeatId
                let state = state |> State.createFeat featId
                state, Cmd.none
            | Msg.RemoveFeat featId ->
                let state = state |> State.removeFeat featId
                state, Cmd.none
            | Msg.FeatValueInputChanged (featId, featValueInput) ->
                let state =
                    state
                    |> State.updateFeat featId (fun feat ->
                        setFeatValue feat featValueInput
                    )
                state, Cmd.none
            | Msg.FeatNameInputChanged (featId, featNameInput) ->
                let state =
                    state
                    |> State.updateFeat featId (fun feat ->
                        setFeatName feat featNameInput
                    )
                state, Cmd.none
        state, cmd @ Cmd.ofSub ^fun _dispatch ->
            onSenseChanged ^ validation {
                let! feats =
                    state.Feats
                    |> Map.values
                    |> Seq.toList
                    |> List.traverseResultA ^fun feat -> validation {
                        let feat = feat.Feat
                        let! featName = getCheckedFeatName state feat |> Result.mapError (fun e -> $"Feat name: {e}")
                        and! featValueSense = feat.Value |> Result.mapError (fun e -> $"Feat value: %A{e}")
                        return featName, featValueSense
                    }
                    |> Result.mapError (List.collect id)
                return Sense ^ senseMap {
                    "feats", senseMap {
                        yield! feats
                    }
                }
            }

[<ReactComponent>]
let FeatsSenseShapeForm (initialSense: Sense) (onSenseChanged: Validation<Sense, string> -> unit) =
    let state, dispatch =
        React.useElmish(
            (fun () -> FeatsSenseShapeForm.init initialSense),
            FeatsSenseShapeForm.update onSenseChanged,
            ()
        )

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
                    textField.size.small
                    textField.onChange (handleFeatNameChanged featId)
                    let featName = FeatsSenseShapeForm.getCheckedFeatName state feat.Feat
                    match featName with
                    | Ok _ -> ()
                    | Error error ->
                        textField.error true
                        textField.helperText error
                ]
                Mui.textField [
                    textField.label "Value"
                    textField.size.small
                    textField.multiline true
                    textField.fullWidth true
                    textField.onChange (handleFeatValueInputChanged featId)
                    match feat.Feat.Value with
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
