namespace Khonsu.Coding.Json.Net

open Khonsu.Coding
open Khonsu.Coding.Json


type JsonValue = Newtonsoft.Json.Linq.JToken

[<AutoOpen>]
module private Helpers =
    type ThothDecoder<'a> = Thoth.Json.Net.Decoder<'a>
    type ThothDecoderError = Thoth.Json.Net.DecoderError
    type ThothJsonValue = string * JsonValue

type ThothJsonDecoder<'a> = Decoder<JsonValue, 'a>

module ThothDecode = Thoth.Json.Net.Decode

type ThothJsonDecoding() =
    
    static let tr2dr (tr: Result<'a, ThothDecoderError>) : DecodeResult<'a> =
        tr |> Result.mapError (fun terr -> DecodeError.Reason $"%A{terr}")
    static let dr2tr (dr: DecodeResult<'a>) : Result<'a, ThothDecoderError> =
        dr |> Result.mapError (fun err -> let msg = $"%A{err}" in msg, Thoth.Json.Net.ErrorReason.FailMessage msg)
    
    static let d2td (d: ThothJsonDecoder<'a>) : ThothDecoder<'a> =
        fun _p v -> d (v) |> dr2tr
    static let td2d (td: ThothDecoder<'a>) : ThothJsonDecoder<'a> =
        fun (v) -> td "X" v |> tr2dr
    
    interface IJsonDecoding<JsonValue> with
        member this.DecodeFromString(input, decoder) = ThothDecode.fromString (d2td decoder) input |> Result.mapError DecodeError.Reason
        member this.String = ThothDecode.string |> td2d
        member this.Float = ThothDecode.float |> td2d
        member this.Int = ThothDecode.int |> td2d
        member this.Array(itemDecoder) = ThothDecode.array (d2td itemDecoder) |> td2d
        member this.Field(fieldName, fieldValueDecoder) = ThothDecode.field fieldName (d2td fieldValueDecoder) |> td2d


// Encoding

module ThothEncode = Thoth.Json.Net.Encode

type ThothJsonEncoding() =
    interface IJsonEncoding<JsonValue> with
        member this.EncodeToString(j) = ThothEncode.toString 0 j
        member this.String(value) = ThothEncode.string value
        member this.Int(value) = ThothEncode.int value
        member this.Float(value) = ThothEncode.float value
        member this.Object(fields) = ThothEncode.object (Seq.toList fields)
        member this.Array(elements) = ThothEncode.array (Seq.toArray elements)
