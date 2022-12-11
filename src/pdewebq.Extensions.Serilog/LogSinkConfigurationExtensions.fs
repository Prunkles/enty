namespace pdewebq.Extensions.Serilog

open System
open System.Runtime.CompilerServices
open Serilog
open Serilog.Configuration
open Serilog.Formatting


[<AutoOpen>]
module LogSinkConfigurationExtensions =
    type LoggerSinkConfiguration with

        member this.MapSourceContext(configure: string -> LoggerSinkConfiguration -> unit, ?sinkMapCountLimit: int) =
            this.Map(
                Serilog.Core.Constants.SourceContextPropertyName,
                "DefaultSourceContext",
                configure,
                ?sinkMapCountLimit=sinkMapCountLimit
            )

        member this.MapDateOnly(configure: DateOnly -> LoggerSinkConfiguration -> unit, ?sinkMapCountLimit: int) =
            this.Map(
                (fun logEvent -> DateOnly.FromDateTime(logEvent.Timestamp.Date)),
                configure,
                ?sinkMapCountLimit=sinkMapCountLimit
            )

// NOTE: Explicit C#-like extensions, because Serilog.Settings searches sealed abstract (i.e. static) classes
[<Extension; Sealed; AbstractClass>]
type LogSinkConfigurationExtensions =
    [<Extension>]
    static member MapDateFile(this: LoggerSinkConfiguration, pathTemplate: string, formatter: ITextFormatter) =
        this.MapDateOnly(
            (fun date wt ->
                let path = pathTemplate.Replace("{Date}", date.ToString("yyyy'-'MM'-'dd"))
                wt.File(formatter, path) |> ignore
            ),
            sinkMapCountLimit=1
        )
