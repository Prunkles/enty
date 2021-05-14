module Khonsu.Coding

type DecodeError =
    | Reason of string
    | Aggregate of reason: string * innerErrors: DecodeError list

type Decoder<'Decodable, 'a> = 'Decodable -> Result<'a, DecodeError>
//type Encoder<'Encoded, 'a> = 'a -> 'Encoded

module Decoder =
    
    let retn x : Decoder<'Encoded, 'a> = fun _ -> Ok x
    
    let bind (binder: 'a -> Decoder<_, 'b>) (decoder: Decoder<_, 'a>) : Decoder<_, 'b> =
        fun decodable -> decoder decodable |> Result.bind (fun x -> (binder x) decodable)


type DecoderBuilder<'Decodable>() =
    member _.Return(x): Decoder<'Decodable, 'a> = Decoder.retn x
    member _.Bind(x, f): Decoder<'Decodable, 'a> = Decoder.bind f x


module Json =
    
    type IJsonDecodable =
        abstract DecodeString: unit -> Result<string, DecodeError>
        abstract DecodeArray: itemDecoder: JsonDecoder<'a> -> Result<'a[], DecodeError>
        abstract DecodeField: fieldName: string * fieldValueDecoder: JsonDecoder<'Field> -> Result<'Field, DecodeError>
    
    and JsonDecoder<'a> = Decoder<IJsonDecodable, 'a>
    
    module Decode =
        
        let string: JsonDecoder<string> = fun j -> j.DecodeString()
        let array itemDecoder : JsonDecoder<'a[]> = fun j -> j.DecodeArray(itemDecoder)
        let field fieldName fieldValueDecoder : JsonDecoder<'a> = fun j -> j.DecodeField(fieldName, fieldValueDecoder)
    
    type JsonDecoderBuilder() =
        member _.Return(x): JsonDecoder<'a> = fun _ -> Ok x
    
    let jsonDecoder = DecoderBuilder<IJsonDecodable>()



module Pg =
    
    open Json
    
    let d =
        jsonDecoder {
            let! x = Decode.field "a" Decode.string
            return x
        }
