namespace enty.Mind.Gateway.Api
//
//open System
//
//open Khonsu.Coding.Json
//open Khonsu.Coding.Json.JsonADecoder.Operators
//
//module Decode = JsonADecode
//module Encode = JsonAEncode
//
//
//type SenseDto<'JsonValue> =
//    | SenseDto of 'JsonValue
//    static member Decoder() = Decode.raw |> JsonADecoder.map SenseDto
//    static member Encoder() = fun (SenseDto sense) -> Encode.raw sense
//
//type EntityDto<'JsonValue> =
//    { Id: Guid
//      Sense: SenseDto<'JsonValue> }
//    static member Decoder() = jsonADecoder {
//        let! id = Decode.field "id" Decode.guid
//        let! sense = Decode.field "sense" (SenseDto.Decoder<_>())
//        return { Id = id; Sense = sense }
//    }
//    static member Encoder() = fun entity -> Encode.object [
//        "id", Encode.guid entity.Id
//        "sense", SenseDto.Encoder<_>() entity.Sense
//    ]
//
//
//type RememberRequest =
//    { EntityId: Guid
//      SenseString: string }
//    static member Decoder() = jsonADecoder {
//        let! eid = Decode.field "eid" Decode.guid
//        let! senseString = Decode.field "senseString" Decode.string
//        return { EntityId = eid; SenseString = senseString }
//    }
//    static member Encoder() = fun rq -> Encode.object [
//        "eid", Encode.guid rq.EntityId
//        "senseString", Encode.string rq.SenseString
//    ]
//
//
//type ForgetRequest =
//    { EntityId: Guid }
//    static member Decoder() = Decode.field "eid" Decode.guid |>> (fun eid -> { EntityId = eid })
//    static member Encoder() = fun rq -> Encode.object [ "eid", Encode.guid rq.EntityId ]
//
//
//type GetEntitiesRequest =
//    { EntityIds: Guid[] }
//    static member Decoder() = jsonADecoder {
//        let! eids = Decode.field "eids" (Decode.array Decode.guid)
//        return { EntityIds = eids }
//    }
//    static member Encoder() = fun rq -> Encode.object [
//        "eids", Encode.array (Array.map Encode.guid rq.EntityIds)
//    ]
//
//type GetEntitiesResponse<'JsonValue> =
//    { Entities: EntityDto<'JsonValue>[] }
//    static member Decoder() = jsonADecoder {
//        let! entities = Decode.field "entities" (Decode.array (EntityDto.Decoder<_>()))
//        return { Entities = entities }
//    }
//    static member Encoder() = fun rp -> Encode.object [
//        "entities", Encode.array (Array.map (EntityDto.Encoder<_>()) rp.Entities)
//    ]
//
//
//type WishRequest =
//    { WishString: string
//      Offset: int; Limit: int }
//    static member Decoder() = jsonADecoder {
//        let! wishString = Decode.field "wishString" Decode.string
//        let! offset, limit = Decode.field "pgn" (jsonADecoder {
//            let! offset = Decode.field "offset" Decode.int
//            let! limit = Decode.field "limit" Decode.int
//            return offset, limit
//        })
//        return { WishString = wishString; Offset = offset; Limit = limit }
//    }
//    static member Encoder() = fun rq -> Encode.object [
//        "wishString", Encode.string rq.WishString
//        "pgn", Encode.object [
//            "offset", Encode.int rq.Offset
//            "limit", Encode.int rq.Limit
//        ]
//    ]
//
//type WishResponse =
//    { EntityIds: Guid[]
//      Total: int }
//    static member Decoder() = jsonADecoder {
//        let! eids = Decode.field "eids" (Decode.array Decode.guid)
//        let! total = Decode.field "pgn" (Decode.field "total" Decode.int)
//        return { EntityIds = eids; Total = total }
//    }
//    static member Encoder() = fun rp -> Encode.object [
//        "eids", Encode.array (rp.EntityIds |> Array.map Encode.guid)
//        "pgn", Encode.object [ "total", Encode.int rp.Total ]
//    ]
//
//
