module rec FApi.JsonScheming

type JsonSchemaObject =
    { Properties: (string * JsonSchema) list
      Required: string list }

type JsonSchemaArray =
    { MinItems: int
      MaxItems: int
      UniqueItems: bool }

type JsonSchema =
    | Null
    | Number
    | Boolean
    | String
    | Object of JsonSchemaObject
    | Array of JsonSchemaArray
