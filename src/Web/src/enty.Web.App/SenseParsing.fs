module enty.Web.App.SenseParsing

open System.Text.RegularExpressions
open enty.Core


[<RequireQualifiedAccess>]
module Sense =

    let parse (input: string) : Result<Sense, string> =
        let elements =
            let pattern = @"""((?:\\\\|\\""|[^""])*?)""|((?:[A-Za-z0-9_-])+)" // unescaped: "((?:\\\\|\\"|[^"])*?)"|((?:[A-Za-z0-9_-])+)
            Regex.Matches(input, pattern)
            |> Seq.map ^fun m ->
                if m.Groups.[1].Success then
                    m.Groups.[1].Value
                else // m.Groups.[2].Success
                    m.Groups.[2].Value
                |> fun input -> Regex.Replace(input, @"\\(\\)|\\("")", @"$1$2")
            |> Seq.toList
        elements |> List.map Sense.Value |> Sense.List |> Ok
