namespace enty.Mind.Parsing.SenseParsing


module Grammar =

    open FParsec

    [<RequireQualifiedAccess>]
    type Expr =
        | Value of string
        | Map of (string * Expr) list
        | List of Expr list

    let comment = pstring "//" >>. skipManyTill anyChar newline
    let ws =
        skipMany (spaces1 <|> comment)
        <?> "whitespace"

    let expr, exprRef = createParserForwardedToRef ()

    let ident =
        let isAsciiId c = isAsciiLetter c || isDigit c || c = '_' || c = '-'
        let options = IdentifierOptions(isAsciiId, isAsciiId)
        identifier options

    let value =
        between (pchar '"') (pchar '"' <?> "closing \"") (manySatisfy (fun c -> c <> '"'))
        <|> ident
        <?> "value"

    let list =
        between (pchar '[') (pchar ']' <?> "closing ]")
            (ws >>. sepEndBy expr ws)
        <?> "list"

    let map =
        let mapElement =
            ident .>> ws .>>. (expr <?> "map element value")
        between (pchar '{') (pchar '}' <?> "closing }")
            (ws >>. sepEndBy mapElement ws)
        <?> "map"

    do exprRef.Value <-
        choice [
            value |>> Expr.Value
            map |>> Expr.Map
            list |>> Expr.List
        ]

[<RequireQualifiedAccess>]
module Sense =

    open FParsec
    open enty.Core
    open Grammar

    let rec private exprToSense (expr: Expr) : Sense =
        match expr with
        | Expr.Value value -> Sense.Value value
        | Expr.List elements -> Sense.List (elements |> List.map exprToSense)
        | Expr.Map els -> els |> Seq.map (fun (k, v) -> k, exprToSense v) |> Map.ofSeq |> Sense.Map

    let parse input =
        let p = ws >>. expr .>> ws
        let result = runParserOnString p () "" input
        match result with
        | Success (r, _, _) -> Result.Ok (exprToSense r)
        | Failure (err, _, _) -> Result.Error err
