module enty.Web.App.SenseParsing

open System
open System.Text
open System.Text.RegularExpressions
open enty.Core


[<RequireQualifiedAccess>]
module Sense =

    type ParseErrorKind =
        | ExpectedIdent
        | ExpectedClosingQuote
        | UnexpectedOpeningQuote
        | UnescapedChar
        | ExpectedEscapedChar

        | Other of string
        | Alts of ParseErrorKind list

    type ParseError =
        { Input: string
          Location: int
          Kind: ParseErrorKind }

    type private ParseState =
        { Input: string
          Location: int }

    [<RequireQualifiedAccess>]
    type private QuoteMode =
        | None
        | Single
        | Double

    module private ParseState =
        let inline goto (newLocation: int) (state: ParseState) : ParseState =
            if newLocation <= state.Input.Length then
                { state with Location = newLocation }
            else
                failwith "Move outside source"

        let inline next (state: ParseState) : ParseState =
            goto (state.Location + 1) state

        let inline isEmpty (state: ParseState) : bool =
            state.Location >= state.Input.Length

        let inline peek (state: ParseState) : Option<char> =
            if state.Location + 1 >= state.Input.Length then None
            else Some state.Input[state.Location + 1]

        let inline error (kind: ParseErrorKind) (state: ParseState) : ParseError =
            { Input = state.Input
              Location = state.Location
              Kind = kind }

        let inline current (state: ParseState) : char =
            state.Input[state.Location]

    let private (|Special|_|) (ch: char) =
            match ch with
            | '{' | '}' | '[' | ']' -> Some ch
            | _ -> None

    let private parseValueStateful (ps: ParseState) : Result<string * ParseState, ParseError> =
        if ParseState.isEmpty ps then
            Error (ParseState.error ParseErrorKind.ExpectedIdent ps)
        else

        let rec parse (ps: ParseState) (quote: QuoteMode) (acc: StringBuilder) : Result<string * ParseState, ParseError> =
            if ParseState.isEmpty ps then
                if quote <> QuoteMode.None
                then Error (ParseState.error ParseErrorKind.ExpectedClosingQuote ps)
                else Ok (acc.ToString(), ps)
            else

            let ch = ParseState.current ps
            match ch with
            | '"' when quote = QuoteMode.Double -> Ok (acc.ToString(), ParseState.next ps)
            | '"' when quote = QuoteMode.None -> Error (ParseState.error ParseErrorKind.UnexpectedOpeningQuote ps)
            | '\'' when quote = QuoteMode.Single -> Ok (acc.ToString(), ParseState.next ps)
            | '\'' when quote = QuoteMode.None -> Error (ParseState.error ParseErrorKind.UnexpectedOpeningQuote ps)
            | '\\' ->
                let escaped = ParseState.next ps
                if ParseState.isEmpty escaped then
                    Error (ParseState.error ParseErrorKind.ExpectedEscapedChar escaped)
                else
                    let inline appendCont (ch: char) =
                        acc.Append(ch) |> ignore
                        parse (ParseState.next escaped) quote acc
                    match ParseState.current escaped with
                    | 'n' -> appendCont '\n'
                    | '\\' | '\''
                    | '\"' | ' '
                    | Special _ as ch -> appendCont ch
                    | _ -> Error (ParseState.error ParseErrorKind.UnescapedChar escaped)
            | ' ' | '\t' | '\n' | Special _ when quote = QuoteMode.None ->
                Ok (acc.ToString(), ps)
            | ch ->
                acc.Append(ch) |> ignore
                parse (ParseState.next ps) quote acc

        let sb = StringBuilder(32)
        match ParseState.current ps with
        | '"' -> parse (ParseState.next ps) QuoteMode.Double sb
        | '\'' -> parse (ParseState.next ps) QuoteMode.Single sb
        | Special _ | ' ' -> Error (ParseState.error ParseErrorKind.ExpectedIdent ps)
        | _ -> parse ps QuoteMode.None sb

    let parseValue (input: string) : Result<string, ParseError> =
        let state: ParseState =
            { Input = input
              Location = 0 }
        let res = parseValueStateful state
        match res with
        | Ok (_value, ps) when not (ParseState.isEmpty ps) ->
            Error (ParseState.error (ParseErrorKind.Other "parseValue input must contains only single ident") ps)
        | Ok (value, _ps) -> Ok value
        | Error e -> Error e

    let parse (input: string) : Result<Sense, string> =
        let elements =
            let pattern = @"""((?:\\\\|\\""|[^""])*?)""|((?:[A-Za-z0-9_-])+)" // unescaped: "((?:\\\\|\\"|[^"])*?)"|((?:[A-Za-z0-9_-])+)
            Regex.Matches(input, pattern)
            |> Seq.map ^fun m ->
                if m.Groups.[1].Success then
                    m.Groups.[1].Value
                else // m.Groups.[2].Success
                    m.Groups.[2].Value
                |> fun input -> Regex.Replace(input, @"\\(\\)|\\("")", @"$1$2")
            |> Seq.toList
        elements |> List.map Sense.Value |> Sense.List |> Ok
