module enty.Core.Parsing.Tests.WishParsingTests

open Xunit
open Swensen.Unquote

open enty.Utils
open enty.Core
open enty.Core.Parsing.WishParsing

let rootEntry = WishPathEntry.MapEntry "r"

let tests: (string * Result<Wish, string -> bool>) list = [
    String.rawMultiline """
        r <a & b>
        """
    , Ok ^ Wish.Operator (
        WishOperator.And (
            Wish.MapFieldIs ([], "r", "a"),
            Wish.MapFieldIs ([], "r", "b")
        )
    )

    // NOTE: Not implemented
    // String.rawMultiline """
    //     r <a b>
    //     """
    // , Ok ^ Wish.Operator (
    //     WishOperator.And (
    //         Wish.AtomIs ([rootEntry], "a"),
    //         Wish.AtomIs ([rootEntry], "b")
    //     )
    // )

    String.rawMultiline """
        r <!(a & b)>
        """
    , Ok ^ Wish.Operator (
        WishOperator.Not (
            Wish.Operator (
                WishOperator.And (
                    Wish.MapFieldIs ([], "r", "a"),
                    Wish.MapFieldIs ([], "r", "b")
                )
            )
        )
    )

    String.rawMultiline """
        tags [ a & b ]
        """
    , Ok ^ Wish.Operator (
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
]

[<Fact>]
let ``Wish parsing`` () : unit =
    for actualInput, expected in tests do
        let actual = Wish.parse actualInput
        match expected with
        | Ok expected -> test <@ actual = Ok expected @>
        | Error expectedErrorF ->
            match actual with
            | Ok actual -> failwith $"'{actualInput}' - expected Error, got %A{actual}"
            | Error actualError ->
                let isErrorExpected = expectedErrorF actualError
                if isErrorExpected then
                    ()
                else
                    failwith $"'%A{actualInput}' - expected an expected Error, got %A{actual}"
