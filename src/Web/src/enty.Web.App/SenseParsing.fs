module enty.Web.App.SenseParsing

open System.Text
open enty.Core


// TODO: Use abstract parse lib

[<RequireQualifiedAccess>]
module rec Sense =

    [<RequireQualifiedAccess>]
    type ParseErrorKind =
        | UnexpectedChar

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

    type ParseError =
        { Input: string
          Location: int
          Kind: ParseErrorKind
          Alt: ParseError option }

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
              Kind = kind
              Alt = None }

        let inline current (state: ParseState) : char =
            state.Input[state.Location]

    let private parseIdentStateful (ps: ParseState) : Result<string * ParseState, ParseError> =
        if ParseState.isEmpty ps then
            Error (ParseState.error ParseErrorKind.ExpectedIdent ps)
        else

        let rec parse (ps: ParseState) (quote: QuoteMode) (acc: StringBuilder) : Result<string * ParseState, ParseError> =
            if ParseState.isEmpty ps then
                if quote <> QuoteMode.None
                then Error (ParseState.error ParseErrorKind.ExpectedQuoteClosing ps)
                else Ok (acc.ToString(), ps)
            else

            let ch = ParseState.current ps
            match ch with
            | '"' when quote = QuoteMode.Double -> Ok (acc.ToString(), ParseState.next ps)
            | '"' when quote = QuoteMode.None -> Error (ParseState.error ParseErrorKind.UnexpectedQuoteOpening ps)
            | '\'' when quote = QuoteMode.Single -> Ok (acc.ToString(), ParseState.next ps)
            | '\'' when quote = QuoteMode.None -> Error (ParseState.error ParseErrorKind.UnexpectedQuoteOpening ps)
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
                    | '\\'
                    | '\'' | '\"'
                    | ' '
                    | '{' | '}' | '[' | ']' as ch -> appendCont ch
                    | _ -> Error (ParseState.error ParseErrorKind.UnescapedChar escaped)
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
        | '{' | '}' | '[' | ']' -> Error (ParseState.error ParseErrorKind.ExpectedIdent ps)
        | _ -> parse ps QuoteMode.None sb

    let private skipWs (ps: ParseState) : ParseState =
        let rec loop (ps: ParseState) : ParseState =
            if ParseState.isEmpty ps then ps
            else
            match (ParseState.current ps) with
            | ' ' | '\t' | '\n' -> loop (ParseState.next ps)
            | _ -> ps
        loop ps

    let private parseExprStateful (ps: ParseState) : Result<Sense * ParseState, ParseError> =
        let maxErr (errs: ParseError list) : ParseError =
           List.maxBy (fun x -> x.Location) errs
        match parseIdentStateful ps with
        | Ok (ident, ps) -> Ok ((Sense.Value ident), ps)
        | Error identErr ->
            match parseListStateful ps with
            | Ok (array, ps) -> Ok (array, ps)
            | Error arrayErr ->
                match parseMapStateful ps with
                | Ok (map, ps) -> Ok (map, ps)
                | Error mapErr ->
                    Error <| maxErr [identErr; arrayErr; mapErr]
                    //Error { identErr with Alt = Some { arrayErr with Alt = Some mapErr } }

    let private parseListStateful (ps: ParseState) : Result<Sense * ParseState, ParseError> =
        let rec loop (ps: ParseState) (acc: Sense list) : Result<Sense * ParseState, ParseError> =
            let ps = skipWs ps
            if ParseState.isEmpty ps then
                Error (ParseState.error ParseErrorKind.ExpectedListClosing ps)
            else
            match ParseState.current ps with
            | ']' -> Ok (Sense.List (List.rev acc), ParseState.next ps)
            | _ ->
                match parseExprStateful ps with
                | Ok (sense, ps) -> loop ps (sense::acc)
                | Error e -> Error e
        if ParseState.isEmpty ps then
            Error (ParseState.error ParseErrorKind.ExpectedList ps)
        else
        match ParseState.current ps with
        | '[' -> loop (ParseState.next ps) []
        | _ -> Error (ParseState.error ParseErrorKind.ExpectedList ps)

    let private parseMapStateful (ps: ParseState) : Result<Sense * ParseState, ParseError> =
        let rec loop (ps: ParseState) (key: string option) (map: Map<string, Sense>) : Result<Sense * ParseState, ParseError> =
            let ps = skipWs ps
            if ParseState.isEmpty ps then
                Error (ParseState.error ParseErrorKind.ExpectedListClosing ps)
            else
            match ParseState.current ps with
            | '}' ->
                match key with
                | None -> Ok (Sense.Map map, ParseState.next ps)
                | Some _ -> Error (ParseState.error ParseErrorKind.ExpectedMapValue ps)
            | _ ->
                match key with
                | None ->
                    match parseIdentStateful ps with
                    | Ok (key, ps) -> loop ps (Some key) map
                    | Error e -> Error e
                | Some key ->
                    match parseExprStateful ps with
                    | Ok (value, ps) -> loop ps None (Map.add key value map)
                    | Error e -> Error e
        if ParseState.isEmpty ps then
            Error (ParseState.error ParseErrorKind.ExpectedMap ps)
        else
        match ParseState.current ps with
        | '{' -> loop (ParseState.next ps) None Map.empty
        | _ -> Error (ParseState.error ParseErrorKind.ExpectedMap ps)

    let parse (input: string) : Result<Sense, string> =
        let ps: ParseState = { Input = input; Location = 0 }
        let ps = skipWs ps
        let res = parseExprStateful ps
        match res with
        | Ok (sense, ps) ->
            let ps = skipWs ps
            if ParseState.isEmpty ps then
                Ok sense
            else
                let e = ParseState.error ParseErrorKind.UnexpectedChar ps
                Error (sprintf $"%A{e}")
        | Error e ->
            Error (sprintf $"%A{e}")
