module enty.Core.Parsing.Tests.SenseParsingTests

open Xunit
open Swensen.Unquote
open enty.Core
open enty.Core.Parsing.SenseParsing
open enty.Utils

let tests: (string * Result<Sense, string -> bool>) list = [
    "k v"
    , Ok ^ sense { yield "k", "v" }

    String.rawMultiline """
        k1 v1
        k2 v2
        """
    , Ok ^ sense { "k1", "v1"; "k2", "v2" }

    "k1 v1 k2 v2"
    , Ok ^ sense { "k1", "v1"; "k2", "v2" }

    String.rawMultiline """
        k1 { a b }
        k2 [ x y z ]
        """
    , Ok ^ sense {
        "k1", senseMap { "a", "b" }
        "k2", senseList { "x"; "y"; "z" }
    }

    ""
    , Ok ^ Sense.empty ()

    "a"
    , Error ^ fun _ -> true

    "{ k v }"
    , Error ^ fun _ -> true
]

[<Fact>]
let ``Parsing`` () =
    for actualInput, expected in tests do
        let actual = Sense.parse actualInput
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
