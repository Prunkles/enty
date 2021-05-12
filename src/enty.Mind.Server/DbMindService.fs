namespace enty.Mind.Server

open System
open System.Linq
open FSharp.Control
open FSharp.Data
open LinqToDB
open LinqToDB.Configuration
open LinqToDB.Data
open LinqToDB.Mapping
open enty.Core
open enty.Mind
open enty.Mind.Server.SenseJson
open enty.Mind.Server.LinqToDbPostgresExtensions


[<Table(Name="Entities")>]
type EntityDao =
    { [<Column "Id">] Id: Guid
      [<Column "Sense">] Sense: obj }

type EntyDataConnection(options: LinqToDbConnectionOptions<EntyDataConnection>) =
    inherit DataConnection(options)
    member this.Entities = this.GetTable<EntityDao>()


type DbMindService(db: EntyDataConnection) =
    
    interface IMindService with
        member this.Remember(EntityId entityId, sense) = async {
            let senseJson =
                sense
                |> Sense.toJson
                |> string
            do! db.Entities
                    .Value((fun x -> x.Id), entityId)
                    .Value((fun x -> x.Sense), ((fun () -> Sql.Json.AsJsonb(senseJson))))
                    .InsertAsync()
                |> Async.AwaitTask
                |> Async.Ignore
            ()
        }
    
        member this.Forget(EntityId entityId) = async {
            let q = query {
                for entity in db.Entities do
                where (entity.Id = entityId)
            }
            
            do! q.DeleteAsync() |> Async.AwaitTask |> Async.Ignore
        }
    
        member this.GetSense(EntityId entityId) = async {
            let q = query {
                for entity in db.Entities do
                where (entity.Id = entityId)
                yield Sql.AsText(entity.Sense)
            }
            let! senseJson = q.FirstAsync() |> Async.AwaitTask
            let sense = JsonValue.Parse(senseJson) |> Sense.ofJson
            return sense
        }
    
        member this.Wish(wish) = asyncSeq {
            let rec queryWish wish =
                match wish with
                | Wish.MapFieldIs (path, key, value) ->
                    let path = path |> List.toArray
                    query {
                        for entity in db.Entities do
                        where (Sql.Json.PathText(entity.Sense, path) = value)
                        select entity
                    }
                | Wish.ListContains (path, value) ->
                    let path = path |> List.toArray
                    query {
                        for e in db.Entities do
                        where (Sql.Json.Contains(Sql.Json.Path(e.Sense, path), value))
                        select e
                    }
                | Wish.Not wish ->
                    let nes = queryWish wish
                    let ids = db.Entities.Select(fun e -> e.Id).Except(nes.Select(fun ne -> ne.Id))
                    query {
                        for e in db.Entities do
                        join id in ids on (e.Id = id)
                        select e
                    }
                | Wish.And (wish1, wish2) ->
                    let e1s = queryWish wish1
                    let e2s = queryWish wish2
                    query {
                        for e1 in e1s do
                        join e2 in e2s
                            on (e1.Id = e2.Id)
                        select e1
                    }
                | Wish.Or (wish1, wish2) ->
                    let e1s = queryWish wish1
                    let e2s = queryWish wish2
                    let ids = e1s.Select(fun e1 -> e1.Id).Union(e2s.Select(fun e2 -> e2.Id)).Distinct()
                    query {
                        for e in db.Entities do
                        join u in ids on (e.Id = u)
                        select e
                    }
            
            let q = query {
                for e in queryWish wish do
                select e.Id
            }
            yield!
                q.AsAsyncEnumerable()
                |> AsyncSeq.ofAsyncEnum
                |> AsyncSeq.map EntityId
        }
