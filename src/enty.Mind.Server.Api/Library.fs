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


type AsyncPagination<'a> =
    abstract Page: page: int * pageSize: int -> Async<'a[] * int * int>

type IMindService =
    abstract Remember: entityId: EntityId * sense: Sense -> Async<unit>
    abstract Forget: entityId: EntityId -> Async<unit>
    
    abstract GetEntities: entityIds: EntityId[] -> Async<Entity[]>
    
    abstract Wish: wish: Wish -> AsyncPagination<EntityId>
