namespace pdewebq.Extensions.Serilog

open System
open System.Text.RegularExpressions
open Serilog
open Serilog.Configuration
open Serilog.Events
open Serilog.Formatting
open Serilog.Templates

[<AutoOpen>]
module LogSinkConfigurationExtensions =
    type LoggerSinkConfiguration with
        member this.MapSourceContextAndDate(templates: Map<string, string>, path: string -> DateOnly -> string, configure: ITextFormatter option -> string -> LoggerSinkConfiguration -> unit) =
            this.Map(
                fun e ->
                    let sourceContext = (e.Properties.["SourceContext"] :?> ScalarValue).Value :?> string
                    sourceContext, DateOnly.FromDateTime(e.Timestamp.Date)
                ,
                fun (sourceContext, date: DateOnly) (wt: LoggerSinkConfiguration) ->
                    let path = path sourceContext date
                    let template =
                        templates
                        |> Map.toSeq
                        |> Seq.tryPick (fun (srcCtxPattern, template) ->
                            if Regex.IsMatch(sourceContext, srcCtxPattern)
                            then Some template else None
                        )
                    match template with
                    | Some template ->
                        let formatter = ExpressionTemplate(template)
                        configure (Some formatter) path wt
                    | None ->
                        configure None path wt
            )
