module enty.Mind.Server.SenseJToken

open System
open enty.Core

[<RequireQualifiedAccess>]
module Sense =

    open Newtonsoft.Json.Linq

    let rec private jTokenToSenseValue (token: JToken) : SenseValue =
        match token.Type with
        | JTokenType.Array ->
            let jArr = token.Value<JArray>()
            let ls = jArr |> Seq.map jTokenToSenseValue |> Seq.toList
            SenseValue.list ls
        | JTokenType.String ->
            let jVal = token.Value<JValue>()
            let value = jVal.ToString()
            SenseValue.atom value
        | JTokenType.Object ->
            let jObj = token.Value<JObject>()
            let mp = jObj.Properties() |> Seq.map (fun jp -> jp.Name, jTokenToSenseValue jp.Value) |> Map.ofSeq
            SenseValue.map mp
        | _ ->
            invalidOp $"Invalid JToken: {token}"

    let rec parseJToken (token: JToken) : Result<Sense, string> =
        jTokenToSenseValue token |> Sense.parseSenseValue

    let rec private senseValueToJToken (senseValue: SenseValue) : JToken =
        match senseValue with
        | SenseValue.Atom (SenseAtom atom) ->
            JValue(atom)
        | SenseValue.List (SenseList ls) ->
            let content = ls |> Seq.map senseValueToJToken |> Seq.toArray
            JArray(content)
        | SenseValue.Map (SenseMap mp) ->
            let content = mp |> Seq.map (fun (KeyValue (k, v)) -> JProperty(k, senseValueToJToken v))
            JObject(content)

    let rec toJToken (sense: Sense) : JToken =
        let senseValue = sense |> Sense.asValue
        senseValueToJToken senseValue
