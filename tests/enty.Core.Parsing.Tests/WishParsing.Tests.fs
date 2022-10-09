module enty.Core.Parsing.Tests.WishParsingTests

open Xunit
open Swensen.Unquote

open enty.Utils
open enty.Core
open enty.Core.Parsing.WishParsing


[<Fact>]
let ``Explicit And`` () : unit =
    let wishString = String.rawMultiline """
        <a & b>
        """
    let wish = Wish.parse wishString
    let expected =
        Wish.Operator (
            WishOperator.And (
                Wish.ValueIs ([], "a"),
                Wish.ValueIs ([], "b")
            )
        )
    test <@ Ok expected = wish @>

[<Fact(Skip="Not implemented")>]
let ``Implicit And`` () : unit =
    let wishString = String.rawMultiline """
        <a b>
        """
    let wish = Wish.parse wishString
    let expected =
        Wish.Operator (
            WishOperator.And (
                Wish.ValueIs ([], "a"),
                Wish.ValueIs ([], "b")
            )
        )
    test <@ Ok expected = wish @>

// ----

[<Fact>]
let ``Parsing <!(a & b)>`` () : unit =
    let wishString = String.rawMultiline """
        <!(a & b)>
        """
    let wish = Wish.parse wishString
    let expected =
        Wish.Operator (
            WishOperator.Not (
                Wish.Operator (
                    WishOperator.And (
                        Wish.ValueIs ([], "a"),
                        Wish.ValueIs ([], "b")
                    )
                )
            )
        )
    test <@ Ok expected = wish @>

[<Fact>]
let ``Complex case #1`` () : unit =
    let wishString = String.rawMultiline """
        { tags [ a & b ] }
        """
    let wish = Wish.parse wishString
    let expected =
        Wish.Operator (
            WishOperator.And (
                Wish.ListContains (
                    [ WishPathEntry.MapEntry "tags" ],
                    "a"
                ),
                Wish.ListContains (
                    [ WishPathEntry.MapEntry "tags" ],
                    "b"
                )
            )
        )
    test <@ Ok expected = wish @>
