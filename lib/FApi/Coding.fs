module FApi.Coding

module Json =
    
    type Decoder<'JsonValue, 'a> = 'JsonValue -> Result<'a, string>
    
    type IJsonValue =
        abstract DecodeString: Decoder<IJsonValue, string>