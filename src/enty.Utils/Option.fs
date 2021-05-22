namespace enty.Utils

[<RequireQualifiedAccess>]
module Option =
    
    let choose opts = Seq.tryPick id opts

