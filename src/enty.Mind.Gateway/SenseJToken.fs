module enty.Mind.SenseJToken

open System
open enty.Core

[<RequireQualifiedAccess>]
module Sense =

    open Newtonsoft.Json.Linq
    
    let rec ofJToken (token: JToken) : Sense =
        match token.Type with
        | JTokenType.Array ->
            let jArr = token.Value<JArray>()
            let ls = jArr |> Seq.map ofJToken |> Seq.toList
            Sense.List ls
        | JTokenType.String ->
            let jVal = token.Value<JValue>()
            let value = jVal.ToString()
            Sense.Value value
        | JTokenType.Object ->
            let jObj = token.Value<JObject>()
            let mp = jObj.Properties() |> Seq.map (fun jp -> jp.Name, ofJToken jp.Value) |> Map.ofSeq
            Sense.Map mp
        | _ -> invalidOp ""
    
    let rec toJToken (sense: Sense) : JToken =
        match sense with
        | Sense.Value value ->
            upcast JValue(value)
        | Sense.List ls ->
            let content = ls |> Seq.map toJToken |> Seq.toArray
            upcast JArray(content)
        | Sense.Map mp ->
            let content = mp |> Seq.map (fun (KeyValue (k, v)) -> JProperty(k, toJToken v))
            upcast JObject(content)
