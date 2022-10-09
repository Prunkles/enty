module enty.Core.Parsing.Tests.SenseParsingTests

open Xunit
open Swensen.Unquote
open enty.Core
open enty.Core.Parsing.SenseParsing
open enty.Utils

let tests = [
    "k v"
    , sense { yield "k", "v" }

    String.rawMultiline """
        k1 v1
        k2 v2
        """
    , sense { "k1", "v1"; "k2", "v2" }

    "k1 v1 k2 v2"
    , sense { "k1", "v1"; "k2", "v2" }

    String.rawMultiline """
        k1 { a b }
        k2 [ x y z ]
        """
    , sense {
        "k1", senseMap { "a", "b" }
        "k2", senseList { "x"; "y"; "z" }
    }
]

[<Fact>]
let ``Parsing`` () =
    for actualInput, expected in tests do
        let actual = Sense.parse actualInput
        test <@ actual = Ok expected @>
