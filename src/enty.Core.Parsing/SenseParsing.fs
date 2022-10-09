namespace enty.Core.Parsing.SenseParsing


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

    let insideList = ws >>. sepEndBy expr ws <?> "inside list"

    let list =
        between (pchar '[') (pchar ']' <?> "closing ]") insideList
        <?> "list"

    let insideMap =
        let mapElement =
            ident .>> ws .>>. (expr <?> "map element value")
        ws >>. sepEndBy mapElement ws
        <?> "inside map"

    let map =
        between (pchar '{') (pchar '}' <?> "closing }") insideMap
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

    let rec private exprToSense (expr: Expr) : SenseEntry =
        match expr with
        | Expr.Value value -> SenseEntry.Value <| SenseValue value
        | Expr.List elements -> SenseEntry.List <| exprListToSense elements
        | Expr.Map els -> SenseEntry.Map <| exprMapToSense els

    and exprListToSense (exprs: Expr list) : SenseList =
        SenseList (exprs |> List.map exprToSense)

    and exprMapToSense (entries: (string * Expr) list) : SenseMap =
        entries |> Seq.map (fun (k, v) -> k, exprToSense v) |> Map.ofSeq |> SenseMap

    let private parseWith parser input =
        let p = ws >>. parser .>> ws
        let result = runParserOnString p () "" input
        match result with
        | Success (r, _, _) -> Result.Ok r
        | Failure (err, _, _) -> Result.Error err

    let parse input = parseWith (expr |>> exprToSense) input
    let parseMap input = parseWith (insideMap |>> exprMapToSense) input
    let parseList input = parseWith (insideList |>> exprListToSense) input
