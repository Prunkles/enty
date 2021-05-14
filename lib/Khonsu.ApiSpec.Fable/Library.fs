namespace FApi.Fable

open FApi.Coding
open FApi.Coding.Json

type ThothDecoder<'a> = Thoth.Json.Decoder<'a>
type ThothDecoderError = Thoth.Json.DecoderError
module ThothDecode = Thoth.Json.Decode

type ThothJsonValue = string * obj

type ThothJsonDecodable(path: string, o: obj) =
    
    static let te2e (thothError: ThothDecoderError) : DecodeError =
        DecodeError.Reason (sprintf $"%A{thothError}")
    
    static let e2te (error: DecodeError) : ThothDecoderError =
        "", Thoth.Json.ErrorReason.FailMessage ""
    
    let callThothDecoder (thothDecoder: ThothDecoder<'a>) : Result<'a, DecodeError> =
        thothDecoder path o |> Result.mapError te2e
    
    static let d2td (decoder: JsonDecoder<'a>) : ThothDecoder<'a> =
        fun path json -> decoder (ThothJsonDecodable (path, json)) |> Result.mapError e2te
    
    static member FromString(decoder: JsonDecoder<'a>, input: string) : Result<'a, DecodeError> =
        ThothDecode.fromString (d2td decoder) input |> Result.mapError DecodeError.Reason
    
    interface IJsonDecodable with
        
        member this.DecodeString() =
            ThothDecode.string |> callThothDecoder
        
        member this.DecodeArray(itemDecoder) =
            ThothDecode.array (d2td itemDecoder) |> callThothDecoder
            
        member this.DecodeField(fieldName, fieldValueDecoder) =
            ThothDecode.field fieldName (d2td fieldValueDecoder) |> callThothDecoder
