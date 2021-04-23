module enty.Storage.FileSystem.Cli.Args

open System
open Argu

[<RequireQualifiedAccess>]
type ReadArgs =
    | [<MainCommand; ExactlyOnce; Last>] EntityId of entityId: Guid
    interface IArgParserTemplate with
        member this.Usage = "TBD"

[<RequireQualifiedAccess>]
type WriteArgs =
    | [<MainCommand; ExactlyOnce; Last>] EntityId of entityId: Guid
    interface IArgParserTemplate with
        member this.Usage = "TBD"

[<RequireQualifiedAccess>]
type DeleteArgs =
    | [<MainCommand; ExactlyOnce; Last>] EntityId of entityId: Guid
    interface IArgParserTemplate with
        member this.Usage = "TBD"

[<RequireQualifiedAccess>]
type FilesArgs =
    | [<SubCommand; MainCommand>] EntityIds of entityIds: Guid list
    | [<SubCommand; CliPrefix(CliPrefix.DoubleDash)>] Stream
    interface IArgParserTemplate with
        member this.Usage = "TBD"

[<RequireQualifiedAccess>]
type StorageArgs =
    | [<CliPrefix(CliPrefix.None)>] Read of ParseResults<ReadArgs>
    | [<CliPrefix(CliPrefix.None)>] Write of ParseResults<WriteArgs>
    | [<CliPrefix(CliPrefix.None)>] Delete of ParseResults<DeleteArgs>
    | [<CliPrefix(CliPrefix.None)>] Files of ParseResults<FilesArgs>
    interface IArgParserTemplate with
        member this.Usage = "TBD"
