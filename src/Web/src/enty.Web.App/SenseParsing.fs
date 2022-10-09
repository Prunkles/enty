module enty.Web.App.SenseParsing

open System.Text
open FsToolkit.ErrorHandling
open enty.Core


// TODO: Use abstract parse lib

[<RequireQualifiedAccess>]
type SenseParseErrorKind =
    | UnexpectedChar

    | ExpectedEntry

    | ExpectedIdent
    | ExpectedQuoteClosing
    | UnexpectedQuoteOpening
    | UnescapedChar
    | ExpectedEscapedChar

    | ExpectedList
    | ExpectedListClosing

    | ExpectedMap
    | ExpectedMapValue
    | ExpectedMapClosing

type SenseParseError =
    { Input: string
      Location: int
      Kind: SenseParseErrorKind
      Alt: SenseParseError option }

[<RequireQualifiedAccess>]
module Sense =

    type private ParseState =
        { Input: string
          Location: int }

    [<RequireQualifiedAccess>]
    type private QuoteMode =
        | None
        | Single
        | Double

    [<RequireQualifiedAccess>]
    module private ParseState =
        let inline goto (newLocation: int) (state: ParseState) : ParseState =
            if newLocation <= state.Input.Length then
                { state with Location = newLocation }
            else
                invalidOp "Move outside source"

        let inline next (state: ParseState) : ParseState =
            goto (state.Location + 1) state

        let inline isEmpty (state: ParseState) : bool =
            state.Location >= state.Input.Length

        let inline peek (state: ParseState) : Option<char> =
            if state.Location + 1 >= state.Input.Length
            then None
            else Some state.Input.[state.Location + 1]

        let inline error (kind: SenseParseErrorKind) (state: ParseState) : SenseParseError =
            { Input = state.Input
              Location = state.Location
              Kind = kind
              Alt = None }

        let inline current (state: ParseState) : char =
            state.Input.[state.Location]

    let private parseIdentStateful (ps: ParseState) : Result<string * ParseState, SenseParseError> =
        if ParseState.isEmpty ps then
            Error (ParseState.error SenseParseErrorKind.ExpectedIdent ps)
        else

        let rec parse (ps: ParseState) (quote: QuoteMode) (acc: StringBuilder) : Result<string * ParseState, SenseParseError> =
            if ParseState.isEmpty ps then
                if quote <> QuoteMode.None
                then Error (ParseState.error SenseParseErrorKind.ExpectedQuoteClosing ps)
                else Ok (acc.ToString(), ps)
            else

            match ParseState.current ps with
            | '"' when quote = QuoteMode.Double -> Ok (acc.ToString(), ParseState.next ps)
            | '"' when quote = QuoteMode.None -> Error (ParseState.error SenseParseErrorKind.UnexpectedQuoteOpening ps)
            | '\'' when quote = QuoteMode.Single -> Ok (acc.ToString(), ParseState.next ps)
            | '\'' when quote = QuoteMode.None -> Error (ParseState.error SenseParseErrorKind.UnexpectedQuoteOpening ps)
            | '\\' ->
                let escaped = ParseState.next ps
                if ParseState.isEmpty escaped then
                    Error (ParseState.error SenseParseErrorKind.ExpectedEscapedChar escaped)
                else
                    let inline appendCont (ch: char) =
                        acc.Append(ch) |> ignore
                        parse (ParseState.next escaped) quote acc
                    match ParseState.current escaped with
                    | 'n' -> appendCont '\n'
                    | '\\'
                    | '\'' | '\"'
                    | ' '
                    | '{' | '}' | '[' | ']' as ch -> appendCont ch
                    | _ -> Error (ParseState.error SenseParseErrorKind.UnescapedChar escaped)
            | ' ' | '\t' | '\n' | '{' | '}' | '[' | ']' when quote = QuoteMode.None ->
                Ok (acc.ToString(), ps)
            | ch ->
                acc.Append(ch) |> ignore
                parse (ParseState.next ps) quote acc

        let sb = StringBuilder(32)
        match ParseState.current ps with
        | '"' -> parse (ParseState.next ps) QuoteMode.Double sb
        | '\'' -> parse (ParseState.next ps) QuoteMode.Single sb
        | ' ' | '\t' | '\n'
        | '{' | '}' | '[' | ']' -> Error (ParseState.error SenseParseErrorKind.ExpectedIdent ps)
        | _ -> parse ps QuoteMode.None sb

    let private skipWs (ps: ParseState) : ParseState =
        let rec loop (ps: ParseState) : ParseState =
            if ParseState.isEmpty ps then ps
            else
            match (ParseState.current ps) with
            | ' ' | '\t' | '\n' -> loop (ParseState.next ps)
            | _ -> ps
        loop ps

    let rec private parseEntryStateful (ps: ParseState) : Result<SenseEntry * ParseState, SenseParseError> =
        let maxErr (errs: SenseParseError list) : SenseParseError =
           List.maxBy (fun x -> x.Location) errs
        match parseIdentStateful ps with
        | Ok (ident, ps) -> Ok (SenseEntry.Value (SenseValue ident), ps)
        | Error identErr ->
            match parseListStateful false ps with
            | Ok (list, ps) -> Ok (SenseEntry.List list, ps)
            | Error arrayErr ->
                match parseMapStateful false ps with
                | Ok (map, ps) -> Ok (SenseEntry.Map map, ps)
                | Error mapErr ->
                    let entryError = ParseState.error SenseParseErrorKind.ExpectedEntry ps
                    Error <| maxErr [entryError; identErr; arrayErr; mapErr]

    and private parseListStateful (rootMode: bool) (ps: ParseState) : Result<SenseList * ParseState, SenseParseError> =
        let rec loop (ps: ParseState) (acc: SenseEntry list) : Result<SenseList * ParseState, SenseParseError> =
            let ps = skipWs ps
            if ParseState.isEmpty ps then
                if rootMode
                then Ok (SenseList (List.rev acc), ps)
                else Error (ParseState.error SenseParseErrorKind.ExpectedListClosing ps)
            else
            match ParseState.current ps with
            | ']' when not rootMode -> Ok (SenseList (List.rev acc), ParseState.next ps)
            | ']' when rootMode -> Error (ParseState.error SenseParseErrorKind.UnexpectedChar ps)
            | _ ->
                match parseEntryStateful ps with
                | Ok (sense, ps) -> loop ps (sense::acc)
                | Error e -> Error e
        if ParseState.isEmpty ps then
            if rootMode
            then Ok (SenseList [], ps)
            else Error (ParseState.error SenseParseErrorKind.ExpectedList ps)
        else
        match ParseState.current ps with
        | '[' when not rootMode -> loop (ParseState.next ps) []
        | _ when rootMode -> loop ps []
        | _ -> Error (ParseState.error SenseParseErrorKind.ExpectedList ps)

    and private parseMapStateful (rootMode: bool) (ps: ParseState) : Result<SenseMap * ParseState, SenseParseError> =
        let rec loop (ps: ParseState) (key: string option) (map: Map<string, SenseEntry>) : Result<SenseMap * ParseState, SenseParseError> =
            let ps = skipWs ps
            if ParseState.isEmpty ps then
                if rootMode then
                    match key with
                    | None -> Ok (SenseMap map, ParseState.next ps)
                    | Some _ -> Error (ParseState.error SenseParseErrorKind.ExpectedMapValue ps)
                else
                    Error (ParseState.error SenseParseErrorKind.ExpectedListClosing ps)
            else
            match ParseState.current ps with
            | '}' when not rootMode ->
                match key with
                | None -> Ok (SenseMap map, ParseState.next ps)
                | Some _ -> Error (ParseState.error SenseParseErrorKind.ExpectedMapValue ps)
            | '}' when rootMode ->
                Error (ParseState.error SenseParseErrorKind.UnexpectedChar ps)
            | _ ->
                match key with
                | None ->
                    match parseIdentStateful ps with
                    | Ok (key, ps) -> loop ps (Some key) map
                    | Error e -> Error e
                | Some key ->
                    match parseEntryStateful ps with
                    | Ok (value, ps) -> loop ps None (Map.add key value map)
                    | Error e -> Error e
        if ParseState.isEmpty ps then
            if rootMode
            then Ok (SenseMap Map.empty, ps)
            else Error (ParseState.error SenseParseErrorKind.ExpectedMap ps)
        else
        match ParseState.current ps with
        | '{' when not rootMode -> loop (ParseState.next ps) None Map.empty
        | _ when rootMode -> loop ps None Map.empty
        | _ -> Error (ParseState.error SenseParseErrorKind.ExpectedMap ps)

    let private parseWith (f: ParseState -> Result<'a * ParseState, SenseParseError>) (input: string) : Result<'a, SenseParseError> =
        let ps: ParseState = { Input = input; Location = 0 }
        let ps = skipWs ps
        let res = f ps
        match res with
        | Ok (res, ps) ->
            let ps = skipWs ps
            if ParseState.isEmpty ps then
                Ok res
            else
                let e = ParseState.error SenseParseErrorKind.UnexpectedChar ps
                Error e
        | Error e ->
            Error e

    let parse (input: string) : Result<SenseEntry, SenseParseError> =
        parseWith parseEntryStateful input

    let parseMap (input: string) : Result<SenseMap, SenseParseError> =
        parseWith (parseMapStateful false) input

    let parseList (input: string) : Result<SenseList, SenseParseError> =
        parseWith (parseListStateful false) input

    let parseIdent (input: string) : Result<string, SenseParseError> =
        parseWith parseIdentStateful input
