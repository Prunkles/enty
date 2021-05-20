namespace Khonsu.Coding

type Encoder<'Encodable, 'a> = 'a -> 'Encodable

[<RequireQualifiedAccess>]
module Encoder =
    
    let mapEncodable (f: 'e0 -> 'e1) (encoder: Encoder<'e0, _>) : Encoder<'e1, _> =
        fun x -> f (encoder x)
