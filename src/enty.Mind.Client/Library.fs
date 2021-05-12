namespace enty.Mind.Client

open enty.Mind

type ServerMindService() =
    interface IMindService with
        member this.Remember(entityId, sense) = failwith "todo"
        member this.GetSense(entityId) = failwith "todo"
        member this.Wish(query) = failwith "todo"
        member this.Forget(entityId) = failwith "todo"
