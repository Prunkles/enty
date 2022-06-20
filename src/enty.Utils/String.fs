[<RequireQualifiedAccess>]
module enty.Utils.String

open System

#if !FABLE_COMPILER
/// <note>
/// Inspired by C# 11 Raw String Literal feature: https://github.com/dotnet/csharplang/blob/main/proposals/raw-string-literal.md
/// </note>
let rawMultiline (str: string) : string =
    let lines = str.Split([|Environment.NewLine|], StringSplitOptions.None)
    if not (lines.[0] = "") then invalidArg (nameof str) "The first line must be empty."
    let lastLine = lines.[lines.Length - 1]
    if lastLine |> Seq.exists ((<>) ' ') then invalidArg (nameof str) "The last line contains non-whitespace symbols"
    let indent = lastLine.Length
    let lines = ArraySegment(lines, 1, lines.Length - 2)
    let unindentedLines =
        seq {
            for idx, line in lines |> Seq.indexed ->
                if line.Length >= indent && String.IsNullOrWhiteSpace(line.Substring(0, indent)) then
                    line.Remove(0, indent)
                elif String.IsNullOrWhiteSpace(line) then
                    ""
                else
                    invalidArg (nameof str) $" The line #{idx} contains symbols out of the last line bound."
        }
    String.Join(Environment.NewLine, unindentedLines)
#endif
