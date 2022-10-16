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
module SenseValue =

    let private isAtomSimple (atom: string) =
        if atom = String.Empty then
            false
        else
            atom
            |> Seq.forall ^fun c ->
                Char.IsLetter(c)
                || Char.IsDigit(c)
                || c = '-' || c = '_'

    let format (senseValue: SenseValue) : string =
        let sb = StringBuilder()
        let rec printSense (sb: StringBuilder) sense =
            match sense with
            | SenseValue.Atom (SenseAtom a) ->
                if isAtomSimple a
                then sb.Append(a) |> ignore
                else sb.Append('"').Append(a).Append('"') |> ignore
            | SenseValue.List (SenseList l) ->
                sb.Append('[') |> ignore
                sb.Append(' ') |> ignore
                for e in l do
                    printSense sb e
                    sb.Append(' ') |> ignore
                sb.Append(']') |> ignore
            | SenseValue.Map (SenseMap m) ->
                sb.Append('{') |> ignore
                sb.Append(' ') |> ignore
                for KeyValue (k, v) in m do
                    sb.Append(k).Append(' ') |> ignore
                    printSense sb v
                    sb.Append(' ') |> ignore
                sb.Append('}') |> ignore
        printSense sb senseValue
        sb.ToString()

    let formatMultiline (senseValue: SenseValue) : string =
        let rec appendSense (sb: StringBuilder) (indent: int) (senseValue: SenseValue) =
            let append (s: string) = sb.Append(s) |> ignore
            let appendLineIndent (s: string) = sb.AppendLine(s).Append(String(' ', 4 * indent)) |> ignore
            let appendLineIndentIndented (s: string) = sb.AppendLine(s).Append(String(' ', 4 * (indent + 1))) |> ignore
            let appendSenseIndented sense = appendSense sb (indent + 1) sense
            let appendSenseValue (value: string) =
                if isAtomSimple value
                then sb.Append(value) |> ignore
                else sb.Append('"').Append(value).Append('"') |> ignore
            match senseValue with
            | SenseValue.Atom (SenseAtom value) ->
                appendSenseValue value
            | SenseValue.List (SenseList list) ->
                match list with
                | [] ->
                    append "[ ]"
                | [ SenseValue.Atom (SenseAtom a) ] ->
                    append "[ "
                    appendSenseValue a
                    append " ]"
                | list ->
                    append "["
                    for value in list do
                        appendLineIndentIndented ""
                        appendSenseIndented value
                    appendLineIndent ""
                    append "]"
            | SenseValue.Map (SenseMap map) ->
                match map with
                | Map.Empty ->
                    sb.Append("{ }") |> ignore
                | Map.ExactlyOne (k, SenseValue.Atom (SenseAtom a)) ->
                    append "{ "
                    append k
                    append " "
                    appendSenseValue a
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
        appendSense sb 0 senseValue
        sb.ToString()


[<RequireQualifiedAccess>]
module Sense =

    let format (sense: Sense) : string =
        SenseValue.format (Sense.asValue sense)

    let formatMultiline (sense: Sense) : string =
        SenseValue.formatMultiline (Sense.asValue sense)

