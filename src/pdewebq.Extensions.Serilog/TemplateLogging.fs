namespace pdewebq.Extensions.Serilog.TemplateLogging

open System.Text.RegularExpressions
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Configuration
open Serilog.Configuration
open Serilog.Core
open Serilog.Events
open pdewebq.Extensions.Serilog


[<RequireQualifiedAccess>]
type LoggingTemplateSourceContext =
    | Exactly of string
    | Pattern of string

type LoggingTemplate =
    { SourceContext: LoggingTemplateSourceContext
      Template: string
      MinLevel: LogEventLevel option }

[<RequireQualifiedAccess>]
module LoggingTemplate =

    let parseConfiguration (templateCfg: IConfiguration) : Validation<LoggingTemplate, unit> = validation {
        let sourceContext =
            let sourceContextSect = templateCfg.GetSection("SourceContext")
            if sourceContextSect.Exists() then
                LoggingTemplateSourceContext.Exactly (sourceContextSect.Get<string>())
            else
                let sourceContextPatternSect = templateCfg.GetSection("SourceContextPattern")
                if sourceContextPatternSect.Exists() then
                    LoggingTemplateSourceContext.Pattern (sourceContextPatternSect.Get<string>())
                else
                    invalidOp "No SourceContext or SourceContextPattern"
        let template = templateCfg.["Template"]
        let minLevel =
            let minLevelSect = templateCfg.GetSection("MinLevel")
            if minLevelSect.Exists()
            then
                let minLevelString = minLevelSect.Get<string>()
                match LogEventLevel.TryParse<LogEventLevel>(minLevelString) with
                | true, e -> Some e
                | false, _ -> invalidOp $"Invalid MinLevel: {minLevelString}"
            else None
        return
            { SourceContext = sourceContext
              Template = template
              MinLevel = minLevel }
    }

    let parseConfigurationMany (configuration: IConfiguration) : Validation<LoggingTemplate list, _> =
        configuration.GetChildren()
        |> Seq.map parseConfiguration
        |> Seq.toList
        |> List.sequenceValidationA


[<AutoOpen>]
module LogSinkConfigurationExtensions =
    type LoggerSinkConfiguration with
        member this.MapTemplates(
            templates: LoggingTemplate list,
            configure: LoggingTemplate -> string -> LoggerSinkConfiguration -> unit
        ) =
            this.MapSourceContext(fun sourceContext wt ->
                let template =
                    templates
                    |> Seq.tryFind (fun template ->
                        match template.SourceContext with
                        | LoggingTemplateSourceContext.Exactly s -> sourceContext = s
                        | LoggingTemplateSourceContext.Pattern pattern -> Regex.IsMatch(sourceContext, pattern)
                    )
                match template with
                | Some template ->
                    configure template sourceContext  wt
                | None ->
                    // NOTE: A dummy sink, because without it MappedSink throws NREs to SelfLog
                    wt.Sink({ new ILogEventSink with member _.Emit(_ev) = () }) |> ignore
            )
