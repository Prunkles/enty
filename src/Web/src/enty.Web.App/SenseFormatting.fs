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

// ----

// TODO: Rename to something more conventional
type StringBuilderF = StringBuilder -> unit

[<RequireQualifiedAccess>]
module StringBuilderF =

    let apply (sb: StringBuilder) (sbf: StringBuilderF) : unit =
        sbf sb

    let run (sbf: StringBuilderF) : string =
        let sb = StringBuilder()
        sbf sb
        string sb

type StringBuilderBuilder() =
    member inline _.Zero(): StringBuilderF =
        fun _sb -> ()
    member inline _.Bind(sbf: StringBuilderF, binder: unit -> StringBuilderF): StringBuilderF =
        fun sb ->
            sbf sb
            let sbf2 = binder ()
            sbf2 sb
    member inline _.For(sequence: 'a seq, body: 'a -> StringBuilderF): StringBuilderF =
        fun sb ->
            for x in sequence do
                let sbf = body x
                sbf sb
    member inline _.Combine(sbf1: StringBuilderF, sbf2: StringBuilderF): StringBuilderF =
        fun sb ->
            sbf1 sb
            sbf2 sb
    member inline _.Delay(f: unit -> StringBuilderF): StringBuilderF =
        fun sb ->
            let sbf = f ()
            sbf sb

let stringBuilderF = StringBuilderBuilder()

// ----

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

    let append (s: string) : StringBuilderF =
        fun sb -> sb.Append(s) |> ignore
    let appendLine () : StringBuilderF =
        fun sb -> sb.AppendLine() |> ignore

    let appendAtom (SenseAtom atom) = stringBuilderF {
        if isAtomSimple atom then
            do! append atom
        else
            do! append "\""
            do! append atom
            do! append "\""
    }

    module Oneline =

        let rec appendMapContent (senseMap: SenseMap) = stringBuilderF {
            let (SenseMap map) = senseMap
            for isLatest, (k, v) in map |> Map.toSeq |> Seq.withIsLatest do
                do! append k
                do! append " "
                do! appendSenseValue v
                if not isLatest then
                    do! append " "
        }

        and appendSenseValue (senseValue: SenseValue) = stringBuilderF {
            match senseValue with
            | SenseValue.Atom atom ->
                do! appendAtom atom
            | SenseValue.List (SenseList l) ->
                do! append "["
                do! append " "
                for e in l do
                    do! appendSenseValue e
                    do! append " "
                do! append "]"
            | SenseValue.Map senseMap ->
                do! append "{"
                do! append " "
                do! appendMapContent senseMap
                do! append "}"
        }

    module Multiline =

        let appendIndent (indent: int) = stringBuilderF {
            do! append (String(' ', 4 * indent))
        }

        let rec appendSenseMapContent (indent: int) (senseMap: SenseMap) = stringBuilderF {
            let (SenseMap map) = senseMap
            for isLatest, (key, value) in map |> Map.toSeq |> Seq.withIsLatest do
                do! appendIndent indent
                do! append key
                do! append " "
                do! appendSenseValue indent value
                if not isLatest then
                    do! appendLine ()
        }

        and appendSenseValue (indent: int) (senseValue: SenseValue) = stringBuilderF {
            match senseValue with
            | SenseValue.Atom atom ->
                do! appendAtom atom
            | SenseValue.List (SenseList list) ->
                match list with
                | [] ->
                    do! append "[ ]"
                | [ SenseValue.Atom atom ] ->
                    do! append "[ "
                    do! appendAtom atom
                    do! append " ]"
                | list ->
                    do! append "["
                    do! appendLine ()
                    for isLatest, value in list |> List.toSeq |> Seq.withIsLatest do
                        do! appendIndent (indent + 1)
                        do! appendSenseValue (indent + 1) value
                        if not isLatest then
                            do! appendLine ()
                    do! appendLine ()
                    do! appendIndent indent
                    do! append "]"
            | SenseValue.Map (SenseMap map as senseMap) ->
                match map with
                | Map.Empty ->
                    do! append "{ }"
                | Map.ExactlyOne (k, SenseValue.Atom atom) ->
                    do! append "{ "
                    do! append k
                    do! append " "
                    do! appendAtom atom
                    do! append " }"
                | _ ->
                    do! append "{"
                    do! appendLine ()
                    do! appendSenseMapContent (indent + 1) senseMap
                    do! appendLine ()
                    do! appendIndent indent
                    do! append "}"
        }


[<RequireQualifiedAccess>]
module SenseValue =

    let format (senseValue: SenseValue) : string =
        SenseValueFormatting.Oneline.appendSenseValue senseValue |> StringBuilderF.run

    let formatMultiline (senseValue: SenseValue) : string =
        SenseValueFormatting.Multiline.appendSenseValue 0 senseValue |> StringBuilderF.run


[<RequireQualifiedAccess>]
module Sense =

    let format (sense: Sense) : string =
        let (Sense senseMap) = sense
        SenseValueFormatting.Oneline.appendMapContent senseMap |> StringBuilderF.run

    let formatMultiline (sense: Sense) : string =
        let (Sense senseMap) = sense
        SenseValueFormatting.Multiline.appendSenseMapContent 0 senseMap |> StringBuilderF.run

