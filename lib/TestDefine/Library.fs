module TestDefine

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

let runtime =
    #if FABLE_COMPILER
    "fable"
    #else
    "dotnet"
    #endif

let result =
    Decode.fromString (Decode.array Decode.int) "[1, 2, 3]"