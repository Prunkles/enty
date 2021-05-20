namespace Khonsu.Coding.Json

open Khonsu.Coding


// --------
// Decoding
// --------

type IJsonDecoding<'JsonValue> =
    abstract DecodeFromString: input: string * decoder: Decoder<'JsonValue, 'a> -> DecodeResult<'a>
    
    abstract String: Decoder<'JsonValue, string>
    abstract Float: Decoder<'JsonValue, float>
    abstract Int: Decoder<'JsonValue, int>
    abstract Array: itemDecoder: Decoder<'JsonValue, 'a> -> Decoder<'JsonValue, 'a[]>
    abstract Field: fieldName: string * fieldValueDecoder: Decoder<'JsonValue, 'a> -> Decoder<'JsonValue, 'a>

type JsonADecoder<'JsonValue, 'a> =  IJsonDecoding<'JsonValue> -> Decoder<'JsonValue, 'a>

[<RequireQualifiedAccess>]
module JsonADecoder =
    
    let retn (x: 'a) : JsonADecoder<'j, 'a> = fun _impl -> Decoder.retn x
    
    let bind (binding: 'a -> JsonADecoder<'j, 'b>) (aDecoder: JsonADecoder<'j, 'a>) : JsonADecoder<'j, 'b> =
        fun impl -> Decoder.bind (fun x -> binding x impl) (aDecoder impl)
    
    let map mapping aDecoder = bind (mapping >> retn) aDecoder
    
    let apply (applier: JsonADecoder<'j, 'a -> 'b>) (decoder: JsonADecoder<'j, 'a>) : JsonADecoder<'j, 'b> =
        fun impl -> fun j ->
            let f = applier impl j
            let a = decoder impl j
            match f, a with
            | Ok f, Ok a ->
                Ok (f a)
            | Error err, Ok _ | Ok _, Error err ->
                Error err
            | Error err0, Error err1 ->
                Error <| DecodeError.Aggregate ("TBD", [err0; err1])
    
    let fail err : JsonADecoder<'j, _> = fun _ _ -> Error err
    
    module Operators =
        let (>>=) x f = bind f x
        let (<!>) f x = map f x
        let (|>>) x f = map f x
        let (<*>) f x = apply f x
        
open JsonADecoder.Operators


[<RequireQualifiedAccess>]
module JsonADecode =

    open System
    
    let raw : JsonADecoder<'j, 'j> = fun _impl -> Ok
    
    let string : JsonADecoder<'j, string> = fun impl -> impl.String
    let float : JsonADecoder<'j, float> = fun impl -> impl.Float
    let int : JsonADecoder<'j, int> = fun impl -> impl.Int
    let array (itemDecoder: JsonADecoder<'j, 'a>) : JsonADecoder<'j, 'a[]> = fun impl -> impl.Array(itemDecoder impl)
    let field fieldName (fieldValueDecoder: JsonADecoder<'j, 'a>) : JsonADecoder<'j, 'a> = fun impl -> impl.Field(fieldName, fieldValueDecoder impl)
    
    let guid: JsonADecoder<'j, Guid> = fun impl ->
        string >>= (fun s ->
            match Guid.TryParse(s) with
            | true, x -> JsonADecoder.retn x
            | _ -> JsonADecoder.fail (DecodeError.Reason "Failed parse guid")
        ) <| impl


type JsonADecoderBuilder() =
    member _.Return(x: 'a): JsonADecoder<'j, 'a> = JsonADecoder.retn x
    member _.Bind(x: JsonADecoder<'j, 'a>, f: 'a -> JsonADecoder<'j, 'b>): JsonADecoder<'j, 'b> = JsonADecoder.bind f x
    member _.MergeSources(d1: JsonADecoder<'j, 'a>, d2: JsonADecoder<'j, 'b>): JsonADecoder<'j, 'a * 'b> =
        (fun x y -> x, y) <!> d1 <*> d2

[<AutoOpen>]
module JsonADecoderBuilder =
    let jsonADecoder = JsonADecoderBuilder()



// --------
// Encoding
// --------

type IJsonEncoding<'JsonValue> =
    abstract EncodeToString: 'JsonValue -> string
    
    abstract String: value: string -> 'JsonValue
    abstract Float: value: float -> 'JsonValue
    abstract Int: value: int-> 'JsonValue
    abstract Object: fields: (string * 'JsonValue) seq -> 'JsonValue
    abstract Array: elements: 'JsonValue seq -> 'JsonValue

type JsonAEncoderValue<'JsonValue> = IJsonEncoding<'JsonValue> -> 'JsonValue
type JsonAEncoder<'JsonValue, 'a> = Encoder<JsonAEncoderValue<'JsonValue>, 'a>

[<RequireQualifiedAccess>]
module JsonAEncode =

    open System
    
    let raw: JsonAEncoder<_, 'j> = fun j -> fun _impl -> j
    
    let string: JsonAEncoder<'j, string> = fun value -> fun impl -> impl.String(value)
    let float: JsonAEncoder<'j, float> = fun value -> fun impl -> impl.Float(value)
    let int: JsonAEncoder<'j, int> = fun value -> fun impl -> impl.Int(value)
    
    let array: JsonAEncoder<_, JsonAEncoderValue<'j>[]> =
        fun elements -> fun impl ->
            let elements = elements |> Array.map (fun v -> v impl)
            impl.Array(elements)
    let object: JsonAEncoder<_, (string * JsonAEncoderValue<'j>) seq> =
        fun fields -> fun impl ->
            let fields: (string * 'j) seq = fields |> Seq.map (fun (k, v) -> k, v impl)
            impl.Object(fields)
    
    let guid: JsonAEncoder<'j, Guid> = fun value -> string (value.ToString())


//

type Bicoder<'v, 'a> =
    { Encoder: Encoder<'v, 'a>
      Decoder: Decoder<'v, 'a> }

module Bicoder =
    
    let ofPair decoder encoder = { Encoder = encoder; Decoder = decoder }
    

type JsonABicoder<'j, 'a> = IJsonDecoding<'j> * IJsonEncoding<'j> -> Bicoder<'j, 'a>

module JsonABicoder =
    
    let ofPair (ade: JsonADecoder<'j, 'a>) (aen: JsonAEncoder<'j, 'a>) : JsonABicoder<'j, 'a> =
        fun (deImpl, enImpl) -> Bicoder.ofPair (ade deImpl) (fun x -> aen x enImpl)
    
    let string: JsonABicoder<'j, string> = fun (dei, eni) -> ofPair JsonADecode.string JsonAEncode.string (dei, eni)
    
    