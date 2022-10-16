module enty.Web.App.SenseFormatting

open System
open System.Text
open enty.Core

[<RequireQualifiedAccess>]
module Seq =

    let withIsLatest (xs: 'a seq) : (bool * 'a) seq =
        // Very naive implementation. TODO refactor
        let n = xs |> Seq.length
        xs |> Seq.indexed |> Seq.map (fun (i, x) ->
            let isLatest = i = (n - 1)
            isLatest, x
        )

[<RequireQualifiedAccess>]
module Map =

    let tryExactlyOne (map: Map<'k, 'v>) : ('k * 'v) option =
        map |> Map.toSeq |> Seq.tryExactlyOne

    let (|ExactlyOne|_|) map =
        tryExactlyOne map

    let (|Empty|NonEmpty|) map =
        if Map.isEmpty map then Empty else NonEmpty


[<RequireQualifiedAccess>]
module SenseValueFormatting =

    let isAtomSimple (atom: string) =
        if atom = String.Empty then
            false
        else
            atom
            |> Seq.forall ^fun c ->
                Char.IsLetterOrDigit(c)
                || c = '-' || c = '_'

    let append (sb: StringBuilder) (s: string) = sb.Append(s) |> ignore
    let appendLine (sb: StringBuilder) = sb.AppendLine() |> ignore

    let appendAtom (sb: StringBuilder) (SenseAtom atom) : unit =
        if isAtomSimple atom then
            append sb atom
        else
            append sb "\""
            append sb atom
            append sb "\""

    module Oneline =

        let rec appendMapContent (sb: StringBuilder) (senseMap: SenseMap) : unit =
            let (SenseMap map) = senseMap
            for isLatest, (k, v) in map |> Map.toSeq |> Seq.withIsLatest do
                append sb k
                append sb " "
                appendSenseValue sb v
                if not isLatest then
                    append sb " "

        and appendSenseValue (sb: StringBuilder) (senseValue: SenseValue) : unit =
            match senseValue with
            | SenseValue.Atom atom ->
                appendAtom sb atom
            | SenseValue.List (SenseList l) ->
                append sb "["
                append sb " "
                for e in l do
                    appendSenseValue sb e
                    append sb " "
                append sb "]"
            | SenseValue.Map senseMap ->
                append sb "{"
                append sb " "
                appendMapContent sb senseMap
                append sb "}"

    module Multiline =

        let appendIndent (sb: StringBuilder) (indent: int) : unit =
            append sb (String(' ', 4 * indent))

        let rec appendSenseMapContent (sb: StringBuilder) (indent: int) (senseMap: SenseMap) : unit =
            let (SenseMap map) = senseMap
            for isLatest, (key, value) in map |> Map.toSeq |> Seq.withIsLatest do
                appendIndent sb indent
                append sb key
                append sb " "
                appendSenseValue sb indent value
                if not isLatest then
                    appendLine sb

        and appendSenseValue (sb: StringBuilder) (indent: int) (senseValue: SenseValue) : unit =
            match senseValue with
            | SenseValue.Atom atom ->
                appendAtom sb atom
            | SenseValue.List (SenseList list) ->
                match list with
                | [] ->
                    append sb "[ ]"
                | [ SenseValue.Atom atom ] ->
                    append sb "[ "
                    appendAtom sb atom
                    append sb " ]"
                | list ->
                    append sb "["
                    appendLine sb
                    for isLatest, value in list |> List.toSeq |> Seq.withIsLatest do
                        appendIndent sb (indent + 1)
                        appendSenseValue sb (indent + 1) value
                        if not isLatest then
                            appendLine sb
                    appendLine sb
                    appendIndent sb indent
                    append sb "]"
            | SenseValue.Map (SenseMap map as senseMap) ->
                match map with
                | Map.Empty ->
                    append sb "{ }"
                | Map.ExactlyOne (k, SenseValue.Atom atom) ->
                    append sb "{ "
                    append sb k
                    append sb " "
                    appendAtom sb atom
                    append sb " }"
                | _ ->
                    append sb "{"
                    appendLine sb
                    appendSenseMapContent sb (indent + 1) senseMap
                    appendLine sb
                    appendIndent sb indent
                    append sb "}"


[<RequireQualifiedAccess>]
module SenseValue =

    let format (senseValue: SenseValue) : string =
        let sb = StringBuilder()
        SenseValueFormatting.Oneline.appendSenseValue sb senseValue
        string sb

    let formatMultiline (senseValue: SenseValue) : string =
        let sb = StringBuilder()
        SenseValueFormatting.Multiline.appendSenseValue sb 0 senseValue
        string sb


[<RequireQualifiedAccess>]
module Sense =

    let format (sense: Sense) : string =
        let (Sense senseMap) = sense
        let sb = StringBuilder()
        SenseValueFormatting.Oneline.appendMapContent sb senseMap
        string sb

    let formatMultiline (sense: Sense) : string =
        let (Sense senseMap) = sense
        let sb = StringBuilder()
        SenseValueFormatting.Multiline.appendSenseMapContent sb 0 senseMap
        string sb

