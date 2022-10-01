module enty.Web.App.SenseCreating.FeatsSenseShapeForm

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

type private FeatId = FeatId of int

[<RequireQualifiedAccess>]
module private FeatsSenseShapeForm =

    [<RequireQualifiedAccess>]
    type Msg =
        | FeatValueInputChanged of FeatId * featValueInput: string
        | FeatNameChanged of FeatId * featName: string
        | CreateFeat
        | RemoveFeat of FeatId

    type FeatValue =
        { Input: string
          Sense: Result<Sense, string> }

    type Feat =
        { Name: string
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
                        Name = featName
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

    let update (msg: Msg) (state: State) : State * Cmd<Msg> =
        match msg with
        | Msg.CreateFeat ->
            let featId = state.LastFeatId |> fun (FeatId i) -> FeatId (i + 1)
            let state = { state with LastFeatId = featId }
            let feat = { Name = ""; Value = { Input = ""; Sense = Error "Empty" } }
            let state = { state with Feats = state.Feats |> Map.add featId feat }
            state, Cmd.none
        | Msg.RemoveFeat featId ->
            let state = { state with Feats = state.Feats |> Map.remove featId }
            state, Cmd.none
        | Msg.FeatValueInputChanged (featId, featValueInput) ->
            let feats =
                state.Feats
                |> Map.change featId (Option.get >> (fun feat ->
                    { feat with Value = FeatValue.parse featValueInput }
                ) >> Some)
            let state = { state with Feats = feats }
            state, Cmd.none
        | Msg.FeatNameChanged (featId, featName) ->
            let feats =
                state.Feats
                |> Map.change featId (Option.get >> (fun feat ->
                    { feat with Name = featName }
                ) >> Some)
            let state = { state with Feats = feats }
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
                |> List.traverseResultA ^fun feat ->
                    match feat.Value.Sense with
                    | Ok sense -> Ok (feat.Name, sense)
                    | Error error -> Error $"Feat {feat.Name} error: {error}"
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
        dispatch (FeatsSenseShapeForm.Msg.FeatNameChanged (featId, featName))

    Mui.stack @+ [
        stack.direction.column
        stack.spacing 1
    ] <| [
        Mui.stack @+ [
            stack.direction.row
            stack.spacing 1
        ] <| [
            Mui.button [
                prop.onClick (fun _e -> dispatch FeatsSenseShapeForm.Msg.CreateFeat)
                prop.text "+"
            ]
        ]
        for featId, feat in state.Feats |> Map.toSeq do
            Mui.stack @+ [
                prop.key (featId |> function FeatId i -> i)
                stack.direction.row
                stack.spacing 1
            ] <| [
                Mui.button [
                    prop.onClick (fun _e -> dispatch (FeatsSenseShapeForm.Msg.RemoveFeat featId))
                    prop.text "-"
                ]
                Mui.textField [
                    textField.label "Name"
                    textField.value feat.Name
                    textField.onChange (handleFeatNameChanged featId)
                ]
                Mui.textField [
                    textField.label "Value"
                    textField.multiline true
                    textField.value feat.Value.Input
                    textField.onChange (handleFeatValueInputChanged featId)
                    match feat.Value.Sense with
                    | Ok _ -> ()
                    | Error error ->
                        textField.error true
                        textField.helperText error
                ]
            ]
    ]
