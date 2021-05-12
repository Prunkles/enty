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

type QueryRequest =
    { Query: JsonValue }

//type QueryResponse =
//    {  }
