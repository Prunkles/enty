namespace pdewebq.Extensions.Serilog

open Serilog.Events

[<RequireQualifiedAccess>]
module LogEvent =

    [<RequiresExplicitTypeArguments>]
    let tryGetScalarProperty<'TProperty> (propertyName: string) (logEvent: LogEvent) : 'TProperty option =
        match logEvent.Properties.TryGetValue(propertyName) with
        | true, (:? ScalarValue as property) ->
            match property.Value with
            | :? 'TProperty as value -> Some value
            | _ -> None
        | _ -> None

    let trySourceContext (logEvent: LogEvent) : string option =
        tryGetScalarProperty<string> Serilog.Core.Constants.SourceContextPropertyName logEvent
