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
    
    { k <v1 | v2> }  <==>  { k v1 | k v 2 }
    
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
        | Value of ValueExpr
        | Map of MapExpr
        | List of ListExpr
    
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
        let element = wishExpr <?> "list element" |>> ListExpr.Element
        
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
    
    let parseExpr input =
        let p = ws >>. wishExpr .>> ws .>> eof
        let result = runParserOnString p () "" input
        match result with
        | Success (expr, _, _) -> Result.Ok expr
        | Failure (err, _, _) -> Result.Error err
    
    let private exprToWish (expr: WishExpr) : Wish =
        
        let rec appendPath path wish =
            match wish with
            | Wish.ValueIs (path', value) -> Wish.ValueIs (path @ path', value)
            | Wish.ListContains (path', element) -> Wish.ListContains (path @ path', element)
            | Wish.MapFieldIs (path', fieldKey, fieldValue) -> Wish.MapFieldIs (path @ path', fieldKey, fieldValue)
            | Wish.Operator op ->
                match op with
                | WishOperator.And (lhs, rhs) -> WishOperator.And (appendPath path lhs, appendPath path rhs)
                | WishOperator.Or (lhs, rhs) -> WishOperator.Or (appendPath path lhs, appendPath path rhs)
                | WishOperator.Not (wish) -> WishOperator.Not (appendPath path wish)
                |> Wish.Operator
        
        let operatorExprToWish mapping operatorExpr =
            match operatorExpr with
            | OperatorExpr.And (lhs, rhs, _) ->
                WishOperator.And (mapping lhs, mapping rhs)
            | OperatorExpr.Or (lhs, rhs) ->
                WishOperator.Or (mapping lhs, mapping rhs)
            | OperatorExpr.Not (valueExpr) ->
                WishOperator.Not (mapping valueExpr)
        
        let rec valueExprToWish (valueExpr: ValueExpr) : Wish =
            match valueExpr with
            | ValueExpr.Value v -> Wish.ValueIs ([], v)
            | ValueExpr.Operator op -> operatorExprToWish valueExprToWish op |> Wish.Operator
        
        and listExprToWish (listExpr: ListExpr) : Wish =
            match listExpr with
            | ListExpr.Element elWishExpr ->
                match elWishExpr with
                | WishExpr.Value valueExpr ->
                    let rec valueExprInListExprToWish valueExpr =
                        match valueExpr with
                        | ValueExpr.Value value ->
                            let path = []
                            Wish.ListContains (path, value)
                        | ValueExpr.Operator opExpr -> opExpr |> operatorExprToWish valueExprInListExprToWish |> Wish.Operator
                    valueExprInListExprToWish valueExpr
                | WishExpr.List listExpr ->
                    listExprToWish listExpr |> appendPath [ WishPathEntry.ListEntry ]
                | WishExpr.Map mapExpr ->
                    mapExprToWish mapExpr |> appendPath [ WishPathEntry.ListEntry ]
            | ListExpr.Operator op -> operatorExprToWish listExprToWish op |> Wish.Operator
            
        and mapExprToWish (mapExpr: MapExpr) : Wish =
            match mapExpr with
            | MapExpr.Field (path, fieldKey, fieldWishExpr) ->
                match fieldWishExpr with
                | WishExpr.Value valueExpr ->
                    let rec valueExprInMapToWish valueExpr =
                        match valueExpr with
                        | ValueExpr.Value value ->
                            let path = path |> List.map WishPathEntry.MapEntry
                            Wish.MapFieldIs (path, fieldKey, value)
                        | ValueExpr.Operator opExpr -> opExpr |> operatorExprToWish valueExprInMapToWish |> Wish.Operator
                    valueExprInMapToWish valueExpr
                | WishExpr.List listExpr ->
                    let path = (path @ [fieldKey]) |> List.map WishPathEntry.MapEntry
                    listExprToWish listExpr |> appendPath path
                | WishExpr.Map mapExpr ->
                    let path = (path @ [fieldKey]) |> List.map WishPathEntry.MapEntry
                    mapExprToWish mapExpr |> appendPath path
            | MapExpr.Operator op -> operatorExprToWish mapExprToWish op |> Wish.Operator
        
        and wishExprToWish (expr: WishExpr) : Wish =
            match expr with
            | WishExpr.Value valueExpr -> valueExprToWish valueExpr
            | WishExpr.List listExpr -> listExprToWish listExpr
            | WishExpr.Map mapExpr -> mapExprToWish mapExpr
        
        wishExprToWish expr
    
    let parse input = parseExpr input |> Result.map exprToWish
