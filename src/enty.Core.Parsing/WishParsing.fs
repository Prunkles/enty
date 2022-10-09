namespace enty.Core.Parsing.WishParsing

open FParsec
open enty.Core

module Grammar =

    (*

    a

    [ <expr> ]
    [ a ]
    [ a & b ]  ==>  [ a b ]
    [ a | b ]

    { <expr> }
    { k=v }  ==>  { k v }
    { k1=v1 & k2=v2 }  ==>  { k1 v1 k2 v2 }

    { k=[ a & b & c ] }  ==>  { k [ a b c ] }

    a  ==  { a }  (top level, no sense)
    k v  ==  { k v }  (top level, have sense)


    // examples
    tags [ olive ]

    sys { file { type image image { withAlpha true } } } tags [ chocolate almond ]

    [ a b c ]    ContextList (And (And (Value a, Value b), Value c))

    (ab)c = a(bc)
    ab = ba

    { k <v1 | v2> }  <==>  { k v1 | k v2 }

    *)


    //? Sense :  { k = v1 }
    //? Wish  :  { k = (v1 | v2) }

    // Sense :  [ a ]
    // Wish  :  [ <a | b> ]  --  contains a value that is 'a' or 'b'
    //          [  a | b  ]  --  contains the value 'a' or the value 'b'

    // a < b c > d
    // (a < b) (c > d)
    // a (< b c >) d
    // ""a | b" | !c"

    type [<RequireQualifiedAccess>]
        OperatorExpr<'SubExpr> =
        | And of lhs: 'SubExpr * rhs: 'SubExpr * implicit: bool
        | Or of lhs: 'SubExpr * rhs: 'SubExpr
        | Not of expr: 'SubExpr

    type [<RequireQualifiedAccess>]
        WishExpr =
        | Atom of AtomExpr
        | Map of MapExpr
        | List of ListExpr
        | Any

    and [<RequireQualifiedAccess>]
        MapExpr =
        | Operator of OperatorExpr<MapExpr>
        | Field of path: string list * key: string * value: WishExpr

    and [<RequireQualifiedAccess>]
        ListExpr =
        | Operator of OperatorExpr<ListExpr>
        | Element of WishExpr

    and [<RequireQualifiedAccess>]
        AtomExpr =
        | Operator of OperatorExpr<AtomExpr>
        | Value of string


    let wishExpr, wishExprRef = createParserForwardedToRef<WishExpr, _> ()

    let ident =
        let isAsciiIdContinue c = isAsciiLetter c || isDigit c || c = '_' || c = '-'
        let isAsciiIdStart c = isAsciiIdContinue c // isAsciiLetter c
        let options = IdentifierOptions(isAsciiIdStart=isAsciiIdStart, isAsciiIdContinue=isAsciiIdContinue)
        identifier options

    let comment = pstring "//" >>. skipManyTill anyChar newline <?> "comment"
    let ws =
        skipMany (spaces1 <|> comment)
        <?> "whitespace"

    let betweenParentheses p = between (pchar '(') (pchar ')') (ws >>. p .>> ws)
    let optBetweenParentheses p = betweenParentheses p <|> p

    let genericOperatorExpr (coreTerm: Parser<'SubExpr, _>) operatorMapping =
        let opp = OperatorPrecedenceParser<'SubExpr, _, _>()

//        let lightAnd, lightAndRef = createParserForwardedToRef ()
//        do lightAndRef :=
//            opp.ExpressionParser .>> ws .>>.? (lightAnd <|> opp.ExpressionParser)
//            |>> fun (x, y) -> OperatorExpr.And (x, y, true)
//            |>> operatorMapping

        let term =
            (coreTerm .>> ws)
            <|> between (pchar '(' >>. ws) (pchar ')' >>. ws)
                opp.ExpressionParser
//            optBetweenParentheses ((lightAnd <|> coreTerm) .>> ws)
//            <|> (betweenParentheses opp.ExpressionParser)
        opp.TermParser <- term
        let orOp = InfixOperator("|", ws, 1, Associativity.Left,
                                 fun x y -> OperatorExpr.Or (x, y) |> operatorMapping)
        let andOp = InfixOperator("&", ws, 2, Associativity.Left,
                                  fun x y -> OperatorExpr.And (x, y, false) |> operatorMapping)
        let notOp = PrefixOperator("!", ws, 3, true,
                                   fun x -> OperatorExpr.Not x |> operatorMapping)

        opp.AddOperator(andOp)
        opp.AddOperator(orOp)
        opp.AddOperator(notOp)

        opp.ExpressionParser .>> ws

    let anyExpr =
        pchar '*'
        |>> ignore
        <?> "any"

    let atomExpr =
        let atom =
            between (pchar '"') (pchar '"' <?> "closing \"")
                (manySatisfy (fun c -> c <> '"'))
            <|> ident
            |>> AtomExpr.Value
        let operator = genericOperatorExpr atom AtomExpr.Operator
        (between (pchar '<' >>. ws) (pchar '>') (operator .>> ws))
        <|> atom
        <?> "value"

    let listExpr =
        let element = wishExpr <?> "list element" |>> ListExpr.Element

        let operator = genericOperatorExpr element ListExpr.Operator

        between (pchar '[' >>. ws) (pchar ']')
            (operator .>> ws)
        <?> "list"

    let innerMapExpr =
        let field =
            let path = many (ident .>> ws .>>? pchar ':' .>> ws)
            let fieldSep = (ws .>>? pchar '=' >>. ws) <|> ws
            path .>>. ident .>> fieldSep .>>. wishExpr
            |>> fun ((path, key), value) -> MapExpr.Field (path, key, value)

        let operator = genericOperatorExpr field MapExpr.Operator
        operator .>> ws <?> "inner map"

    let mapExpr =
        between (pchar '{'  >>. ws) (pchar '}') innerMapExpr
        <?> "map"

    do wishExprRef.Value <-
        choice [
            anyExpr |>> fun () -> WishExpr.Any
            listExpr |>> WishExpr.List
            mapExpr |>> WishExpr.Map
            atomExpr |>> WishExpr.Atom
        ]


open Grammar

[<RequireQualifiedAccess>]
module Wish =

    let private parseWith parser input =
        let p = ws >>. parser .>> ws .>> eof
        let result = runParserOnString p () "" input
        match result with
        | Success (expr, _, _) -> Result.Ok expr
        | Failure (err, _, _) -> Result.Error err

    let parseExpr input =
        parseWith wishExpr input

    let private exprToWish (expr: WishExpr) : Wish =

        let rec appendPath path wish =
            match wish with
            | Wish.AtomIs (path', value) -> Wish.AtomIs (path @ path', value)
            | Wish.ListContains (path', element) -> Wish.ListContains (path @ path', element)
            | Wish.MapFieldIs (path', fieldKey, fieldValue) -> Wish.MapFieldIs (path @ path', fieldKey, fieldValue)
            | Wish.Any path' -> Wish.Any (path @ path')
            | Wish.Operator op ->
                match op with
                | WishOperator.And (lhs, rhs) -> WishOperator.And (appendPath path lhs, appendPath path rhs)
                | WishOperator.Or (lhs, rhs) -> WishOperator.Or (appendPath path lhs, appendPath path rhs)
                | WishOperator.Not wish -> WishOperator.Not (appendPath path wish)
                |> Wish.Operator

        let operatorExprToWish mapping operatorExpr =
            match operatorExpr with
            | OperatorExpr.And (lhs, rhs, _) ->
                WishOperator.And (mapping lhs, mapping rhs)
            | OperatorExpr.Or (lhs, rhs) ->
                WishOperator.Or (mapping lhs, mapping rhs)
            | OperatorExpr.Not valueExpr ->
                WishOperator.Not (mapping valueExpr)

        let rec atomExprToWish (atomExpr: AtomExpr) : Wish =
            match atomExpr with
            | AtomExpr.Value v -> Wish.AtomIs ([], v)
            | AtomExpr.Operator op -> operatorExprToWish atomExprToWish op |> Wish.Operator

        and listExprToWish (listExpr: ListExpr) : Wish =
            match listExpr with
            | ListExpr.Element elWishExpr ->
                match elWishExpr with
                | WishExpr.Atom atomExpr ->
                    let rec valueExprInListExprToWish atomExpr =
                        match atomExpr with
                        | AtomExpr.Value value ->
                            let path = []
                            Wish.ListContains (path, value)
                        | AtomExpr.Operator opExpr -> opExpr |> operatorExprToWish valueExprInListExprToWish |> Wish.Operator
                    valueExprInListExprToWish atomExpr
                | WishExpr.List listExpr ->
                    listExprToWish listExpr |> appendPath [ WishPathEntry.ListEntry ]
                | WishExpr.Map mapExpr ->
                    mapExprToWish mapExpr |> appendPath [ WishPathEntry.ListEntry ]
                | WishExpr.Any ->
                    Wish.Any [ WishPathEntry.ListEntry ]
            | ListExpr.Operator op -> operatorExprToWish listExprToWish op |> Wish.Operator

        and mapExprToWish (mapExpr: MapExpr) : Wish =
            match mapExpr with
            | MapExpr.Field (path, fieldKey, fieldWishExpr) ->
                match fieldWishExpr with
                | WishExpr.Atom atomExpr ->
                    let rec valueExprInMapToWish atomExpr =
                        match atomExpr with
                        | AtomExpr.Value value ->
                            let path = path |> List.map WishPathEntry.MapEntry
                            Wish.MapFieldIs (path, fieldKey, value)
                        | AtomExpr.Operator opExpr -> opExpr |> operatorExprToWish valueExprInMapToWish |> Wish.Operator
                    valueExprInMapToWish atomExpr
                | WishExpr.List listExpr ->
                    let path = (path @ [fieldKey]) |> List.map WishPathEntry.MapEntry
                    listExprToWish listExpr |> appendPath path
                | WishExpr.Map mapExpr ->
                    let path = (path @ [fieldKey]) |> List.map WishPathEntry.MapEntry
                    mapExprToWish mapExpr |> appendPath path
                | WishExpr.Any ->
                    let path = (path @ [fieldKey]) |> List.map WishPathEntry.MapEntry
                    Wish.Any path
            | MapExpr.Operator op -> operatorExprToWish mapExprToWish op |> Wish.Operator

        and wishExprToWish (expr: WishExpr) : Wish =
            match expr with
            | WishExpr.Atom atomExpr -> atomExprToWish atomExpr
            | WishExpr.List listExpr -> listExprToWish listExpr
            | WishExpr.Map mapExpr -> mapExprToWish mapExpr
            | WishExpr.Any -> Wish.Any []

        wishExprToWish expr

    let parse input = parseExpr input |> Result.map exprToWish
    let parseMap input = parseWith innerMapExpr input |> Result.map (WishExpr.Map >> exprToWish)
