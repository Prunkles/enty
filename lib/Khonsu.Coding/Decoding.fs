namespace Khonsu.Coding

[<RequireQualifiedAccess>]
type DecodeError =
    | Reason of string
    | Aggregate of reason: string * innerErrors: DecodeError list

type DecodeResult<'a> = Result<'a, DecodeError>

type Decoder<'Decodable, 'a> = 'Decodable -> DecodeResult<'a>

[<RequireQualifiedAccess>]
module Decoder =
    
    let mapDecodable (f: 'd1 -> 'd0) (decoder: Decoder<'d0, _>) : Decoder<'d1, _> =
        fun d -> decoder (f d)
    
    let retn x : Decoder<'Encoded, 'a> = fun _v -> Ok x
    
    let bind (binder: 'a -> Decoder<_, 'b>) (decoder: Decoder<_, 'a>) : Decoder<_, 'b> =
        fun decodable -> decoder decodable |> Result.bind (fun x -> (binder x) decodable)
    
    let map mapping decoder = bind (mapping >> retn) decoder
    
    let succeed (x: 'a) : Decoder<_, 'a> = fun _ -> Ok x
