module enty.Storage.FileSystem.Mime

open Microsoft.AspNetCore.StaticFiles
open enty.Core


type ISenseMimeParser =
    abstract Parse: sense: Sense -> string option


type SenseMimeParser() =
    let provider = FileExtensionContentTypeProvider()
    interface ISenseMimeParser with
        member this.Parse(sense) =
            match sense |> Sense.tryGet "file:mime" with
            | None ->
                match sense |> Sense.tryGet "file:name" with
                | None -> None
                | Some filename ->
                    match provider.TryGetContentType(filename) with
                    | true, mime -> Some mime
                    | false, _ -> None
            | Some mime ->
                Some mime
