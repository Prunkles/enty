module enty.Web.App.SenseFormatting

open System
open System.Text
open enty.Core

[<RequireQualifiedAccess>]
module Map =

    let tryExactlyOne (map: Map<'k, 'v>) : ('k * 'v) option =
        map |> Map.toSeq |> Seq.tryExactlyOne

    let (|ExactlyOne|_|) map =
        tryExactlyOne map

    let (|Empty|NonEmpty|) map =
        if Map.isEmpty map then Empty else NonEmpty


[<RequireQualifiedAccess>]
module Sense =

    let private isValueSimple (value: string) =
        value
        |> Seq.forall ^fun c ->
            Char.IsLetter(c)
            || Char.IsDigit(c)
            || c = '-' || c = '_'

    let format (sense: Sense) : string =
        let sb = StringBuilder()
        let rec printSense (sb: StringBuilder) sense =
            match sense with
            | Sense.Value v ->
                if isValueSimple v
                then sb.Append(v) |> ignore
                else sb.Append('"').Append(v).Append('"') |> ignore
            | Sense.List l ->
                sb.Append('[') |> ignore
                sb.Append(' ') |> ignore
                for e in l do
                    printSense sb e
                    sb.Append(' ') |> ignore
                sb.Append(']') |> ignore
            | Sense.Map m ->
                sb.Append('{') |> ignore
                sb.Append(' ') |> ignore
                for KeyValue (k, v) in m do
                    sb.Append(k).Append(' ') |> ignore
                    printSense sb v
                    sb.Append(' ') |> ignore
                sb.Append('}') |> ignore
        printSense sb sense
        sb.ToString()

    let formatMultiline (sense: Sense) : string =
        let rec appendSense (sb: StringBuilder) (indent: int) (sense: Sense) =
            let append (s: string) = sb.Append(s) |> ignore
            let appendLineIndent (s: string) = sb.AppendLine(s).Append(String(' ', 4 * indent)) |> ignore
            let appendLineIndentIndented (s: string) = sb.AppendLine(s).Append(String(' ', 4 * (indent + 1))) |> ignore
            let appendSenseIndented sense = appendSense sb (indent + 1) sense
            let appendSenseValue (value: string) =
                if isValueSimple value
                then sb.Append(value) |> ignore
                else sb.Append('"').Append(value).Append('"') |> ignore
            match sense with
            | Sense.Value value ->
                appendSenseValue value
            | Sense.List list ->
                match list with
                | [] ->
                    append "[ ]"
                | [ Sense.Value v ] ->
                    append "[ "
                    appendSenseValue v
                    append " ]"
                | list ->
                    append "["
                    for value in list do
                        appendLineIndentIndented ""
                        appendSenseIndented value
                    appendLineIndent ""
                    append "]"
            | Sense.Map map ->
                match map with
                | Map.Empty ->
                    sb.Append("{ }") |> ignore
                | Map.ExactlyOne (k, Sense.Value v) ->
                    append "{ "
                    append k
                    append " "
                    appendSenseValue v
                    append " }"
                | map ->
                    append "{"
                    for KeyValue (key, value) in map do
                        appendLineIndentIndented ""
                        append key
                        append " "
                        appendSenseIndented value
                    appendLineIndent ""
                    append "}"
        let sb = StringBuilder()
        appendSense sb 0 sense
        sb.ToString()
