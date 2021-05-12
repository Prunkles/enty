module enty.Mind.WishParsing

open FParsec


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
    
    
    [<RequireQualifiedAccess>]
    type WishExpr =
        | Value of ValueExpr
        | Map of MapExpr
        | List of ListExpr
    
    and [<RequireQualifiedAccess>]
        OperatorExpr<'SubExpr> =
        | And of lhs: 'SubExpr * rhs: 'SubExpr * implicit: bool
        | Or of lhs: 'SubExpr * rhs: 'SubExpr
        | Not of expr: 'SubExpr
    
    and [<RequireQualifiedAccess>]
        MapExpr =
        | Operator of OperatorExpr<MapExpr>
        | Field of path: string list * key: string * value: WishExpr
    
    and [<RequireQualifiedAccess>]
        ListExpr =
        | Operator of OperatorExpr<ListExpr>
        | Element of WishExpr
    
    and [<RequireQualifiedAccess>]
        ValueExpr =
        | Operator of OperatorExpr<ValueExpr>
        | Value of string
    
    
    let wishExpr, wishExprRef = createParserForwardedToRef<WishExpr, _> ()
    
    let ident =
        let isAsciiIdStart c = isAsciiLetter c
        let isAsciiIdContinue c = isAsciiLetter c || isDigit c || c = '_'
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
    
    let valueExpr =
        let value =
            between (pchar '"') (pchar '"' <?> "closing \"")
                (manySatisfy (fun c -> c <> '"'))
            <|> ident
            |>> ValueExpr.Value
        let operator = genericOperatorExpr value ValueExpr.Operator
        (between (pchar '<' >>. ws) (pchar '>') (operator .>> ws))
        <|> value
        <?> "value"
    
    
    let listExpr =
        let element = wishExpr |>> ListExpr.Element
        
        let operator = genericOperatorExpr element ListExpr.Operator
        
        between (pchar '[' >>. ws) (pchar ']')
            (operator .>> ws)
        <?> "list"
    
    let mapExpr =
        let field =
            let path = many (ident .>> ws .>>? pchar ':' .>> ws)
            let fieldSep = (ws .>>? pchar '=' >>. ws) <|> ws
            path .>>. ident .>> fieldSep .>>. wishExpr
            |>> fun ((path, key), value) -> MapExpr.Field (path, key, value)
        
        let operator = genericOperatorExpr field MapExpr.Operator
        
        between (pchar '{'  >>. ws) (pchar '}')
            (operator .>> ws)
        <?> "map"
    
    do wishExprRef :=
        choice [
            listExpr |>> WishExpr.List
            mapExpr |>> WishExpr.Map
            valueExpr |>> WishExpr.Value
        ]
    


module Wish =
    
    open FParsec
    open Grammar
    
    let rec private parsedToWish (expr: WishExpr) =
        expr
    
    let parse input =
        let p = ws >>. wishExpr .>> ws .>> eof
        let result = runParserOnString p () "" input
        match result with
        | Success (p, _, _) -> Result.Ok (parsedToWish p)
        | Failure (err, _, _) -> Result.Error err
