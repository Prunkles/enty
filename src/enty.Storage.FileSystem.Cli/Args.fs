module enty.Storage.FileSystem.Cli.Args

open System

[<RequireQualifiedAccess>]
type ReadArgs =
    { EntityId: Guid }

[<RequireQualifiedAccess>]
type WriteArgs =
    { EntityId: Guid }

[<RequireQualifiedAccess>]
type DeleteArgs =
    { EntityId: Guid }

[<RequireQualifiedAccess>]
type FilesArgs =
    | List of entityIds: Guid list
    | Stream

[<RequireQualifiedAccess>]
type StorageArgs =
    | Read of ReadArgs
    | Write of WriteArgs
    | Delete of DeleteArgs
    | Files of FilesArgs
