namespace enty.Core.Traits

open System
open enty.Core

[<AutoOpen>]
module Utils =
    
    let (|MapContains|_|) key (m: Map<'k, 'v>) = m |> Map.tryFind key

    let (|ByPath|_|) path sense = Sense.tryGet path sense
    

module Sense =

    module Feature =
    
//        module Data =
//            let hasData (sense: Sense) : unit option =
//                match sense with
//                | ByPath ["feature";"data"; "has-data"] (Sense.Value "T") -> Some ()
//                | _ -> None
        
        module Tags =
            
            let tags = function
                | ByPath ["feature";"tags"] (Sense.List ls) ->
                    Some [
                        for s in ls do
                            match s with
                            | Sense.Value x -> yield x
                            | _ -> ()
                    ]
                | _ -> None
        
        module File =
            
            let private root = ["feature";"file"]
            let private pathTo propName = root @ [propName]
            
            let private getSense (sense: Sense) (prop: string) = 
                Sense.tryGet (pathTo prop) sense
            
            let private getValue (sense: Sense) (prop: string) = 
                getSense sense prop
                |> Option.bind (function Sense.Value x -> Some x | _ -> None)
            
            let filename (sense: Sense) =
                getValue sense "filename"
        
            module Mime =
                
                let mime = function
                    | ByPath ["feature";"file";"mime"] (Sense.Value mime) -> Some mime
                    | _ -> None
        
        module Image =
            
            let preview sense =
                sense |> Sense.tryGetValue ["feature";"image";"preview"]
                |> Option.map (Guid.Parse >> EntityId)
