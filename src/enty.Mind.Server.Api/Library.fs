namespace enty.Mind.Server.Api

open System
open FSharp.Data

type AddRequest =
    { EntityId: Guid
      Sense: JsonValue }

type RemoveRequest =
    { EntityId: Guid }

type GetSenseRequest =
    { EntityId: Guid }

type GetSenseResponse =
    { Sense: JsonValue }

type PaginationRequest =
    { Page: int
      PageSize: int }

type PaginationResponse =
    { Page: int
      PageSize: int
      Pages: int }

type WishRequest =
    { WishString: string
      Pagination: PaginationRequest }

type WishResponse =
    { EntityIds: Guid[]
      Pagination: PaginationResponse }
