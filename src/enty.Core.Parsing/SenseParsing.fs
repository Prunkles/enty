namespace enty.Core.Parsing.SenseParsing


module Grammar =

    open FParsec

    [<RequireQualifiedAccess>]
    type Expr =
        | Atom of string
        | Map of (string * Expr) list
        | List of Expr list

    let comment = pstring "//" >>. skipManyTill anyChar newline
    let ws =
        skipMany (spaces1 <|> comment)
        <?> "whitespace"

    let expr, exprRef = createParserForwardedToRef ()

    let ident =
        let isAsciiId c = isAsciiLetter c || isDigit c || c = '_' || c = '-'
        identifier (IdentifierOptions(isAsciiId, isAsciiId))

    let atom =
        between (pchar '"') (pchar '"' <?> "closing \"")
            (manySatisfy (fun c -> c <> '"'))
        <|> ident
        <?> "value"

    let insideList =
        sepEndBy expr ws

    let list =
        between (pchar '[') (pchar ']' <?> "closing ]")
            (ws >>. insideList)
        <?> "list"

    let insideMap =
        let mapElement =
            ident .>> ws .>>. (expr <?> "map element value")
        sepEndBy mapElement ws

    let map =
        between (pchar '{') (pchar '}' <?> "closing }")
            (ws >>. insideMap)
        <?> "map"

    do exprRef.Value <-
        choice [
            atom |>> Expr.Atom
            map |>> Expr.Map
            list |>> Expr.List
        ]

[<RequireQualifiedAccess>]
module Sense =

    open FParsec
    open enty.Core
    open Grammar

    let rec private exprToSenseValue (expr: Expr) : SenseValue =
        match expr with
        | Expr.Atom value -> SenseValue.Atom <| SenseAtom value
        | Expr.List elements -> SenseValue.List <| exprListToSenseList elements
        | Expr.Map els -> SenseValue.Map <| exprMapToSenseMap els

    and private exprListToSenseList (exprs: Expr list) : SenseList =
        SenseList (exprs |> List.map exprToSenseValue)

    and private exprMapToSenseMap (entries: (string * Expr) list) : SenseMap =
        entries |> Seq.map (fun (k, v) -> k, exprToSenseValue v) |> Map.ofSeq |> SenseMap

    let private parseWith parser input =
        let p = ws >>. parser .>> ws .>> eof
        let result = runParserOnString p () "" input
        match result with
        | Success (r, _, _) -> Result.Ok r
        | Failure (err, _, _) -> Result.Error err

    let parse input : Result<Sense, _> =
        parseWith (insideMap |>> exprMapToSenseMap) input |> Result.map Sense
