namespace enty.Mind.Server

open System
open System.Linq
open LinqToDB
open LinqToDB.Configuration
open LinqToDB.Data
open LinqToDB.Mapping
open Microsoft.Extensions.Logging
open Newtonsoft.Json.Linq
open enty.Core
open enty.Mind
open enty.Mind.Server
open enty.Mind.Server.SenseJToken
open enty.Mind.Server.LinqToDbPostgresExtensions


[<Table(Name="entities")>]
[<CLIMutable>]
type EntityDao = {
    [<Column "id"; PrimaryKey; Identity>] Id: Guid
    [<Column "sense"; NotNull>] Sense: obj
    [<Column "created_dts"; NotNull>] CreatedDts: DateTime
    [<Column "updated_dts"; NotNull>] UpdatedDts: DateTime
}

type EntyDataConnection(options: LinqToDbConnectionOptions<EntyDataConnection>) =
    inherit DataConnection(options)
    member this.Entities = this.GetTable<EntityDao>()


type DbMind(logger: ILogger<DbMind>, db: EntyDataConnection) =

    interface IMind with
        member this.Remember(EntityId entityId, sense) = async {
            logger.LogInformation("Remembering entity {EntityId}", entityId)
            logger.LogTrace("Remembering entity {EntityId} with sense {@Sense}", entityId, sense)
            let senseJson = (Sense.toJToken sense).ToString()
            let dts = DateTime.Now
            do! db.ExecuteAsync("""
                    INSERT INTO entities (id, sense, created_dts, updated_dts)
                    VALUES (@id, @sense, @dts, @dts)
                    ON CONFLICT (id) DO UPDATE
                        SET updated_dts = excluded.updated_dts,
                            sense = excluded.sense
                    """,
                    DataParameter("id", entityId, DataType.Guid),
                    DataParameter("sense", senseJson, DataType.BinaryJson),
                    DataParameter("dts", dts, DataType.Timestamp)
                )
                |> Async.AwaitTask
                |> Async.Ignore
        }

        member this.Forget(EntityId entityId) = async {
            logger.LogInformation("Forgetting entity {EntityId}", entityId)
            let q = query {
                for entity in db.Entities do
                where (entity.Id = entityId)
            }

            do! q.DeleteAsync() |> Async.AwaitTask |> Async.Ignore
        }

        member this.GetEntities(eids) = async {
            logger.LogInformation("Getting entities {@EntityIds}", eids)
            let eids = eids |> Seq.map (fun (EntityId x) -> x)
            let q = query {
                for entity in db.Entities do
                where (eids.Contains(entity.Id))
                select (entity.Id, Sql.AsText(entity.Sense))
            }
            let! entityDaos = q.ToListAsync() |> Async.AwaitTask
            let entities =
                entityDaos
                |> Seq.map (fun (eid, senseString) ->
                    { Id = EntityId eid
                      Sense = JToken.Parse(senseString) |> Sense.ofJToken }
                )
                |> Seq.toArray

            return entities
        }

        member this.Wish(wish, offset, limit) = async {
            logger.LogInformation("Wishing +{Offset}-{Limit} entities", offset, limit)
            logger.LogTrace("Wishing +{Offset}-{Limit} entities by {@Wish}", offset, limit, wish)
            let selectEntitiesByIds ids = query {
                for e in db.Entities do
                join id in ids on (e.Id = id)
                select e
            }
            let stringPath path =
                path
                |> Seq.map ^function
                    | WishPathEntry.ListEntry -> "[*]"
                    | WishPathEntry.MapEntry key -> $".{key}"
                |> String.concat ""
            let selectEntitiesByJsonpath jsonpath = query {
                for entity in db.Entities do
                where (Sql.Json.op_AtAt(entity.Sense, jsonpath))
                select entity
            }
            let selectEntitiesByJsonpathQ jsonpath = query {
                for entity in db.Entities do
                where (Sql.Json.op_AtQmark(entity.Sense, jsonpath))
                select entity
            }

            let rec queryWish wish =
                match wish with
                | Wish.ValueIs (path, value) ->
                    let path = stringPath path
                    let jsonpath = $"${path} == \"{value}\""
                    selectEntitiesByJsonpath jsonpath
                | Wish.MapFieldIs (path, key, value) ->
                    let path = stringPath path
                    let jsonpath = $"${path}.{key} == \"{value}\""
                    selectEntitiesByJsonpath jsonpath
                | Wish.ListContains (path, value) ->
                    let path = stringPath path
                    let jsonpath = $"${path}[*] == \"{value}\""
                    selectEntitiesByJsonpath jsonpath
                | Wish.Any path ->
                    let path = stringPath path
                    let jsonpath = $"${path}"
                    selectEntitiesByJsonpathQ jsonpath
                | Wish.Operator wishOperator -> queryWishOperator wishOperator

            and queryWishOperator wishOperator =
                match wishOperator with
                | WishOperator.Not wish ->
                    let nes = queryWish wish
                    let ids =
                        db.Entities.Select(fun e -> e.Id)
                            .Except(nes.Select(fun ne -> ne.Id))
                    selectEntitiesByIds ids
                | WishOperator.And (wish1, wish2) ->
                    let e1s = queryWish wish1
                    let e2s = queryWish wish2
                    let ids = query {
                        for e1 in e1s do
                        join e2 in e2s
                            on (e1.Id = e2.Id)
                        select e1.Id
                    }
                    selectEntitiesByIds (ids.Distinct())
                | WishOperator.Or (wish1, wish2) ->
                    let e1s = queryWish wish1
                    let e2s = queryWish wish2
                    let ids =
                        e1s.Select(fun e1 -> e1.Id)
                            .Union(e2s.Select(fun e2 -> e2.Id))
                            .Distinct()
                    selectEntitiesByIds ids

            let q = query {
                for e in queryWish wish do
                select e.Id
            }

            let! total = q.CountAsync() |> Async.AwaitTask
            let! eids = q.Skip(offset).Take(limit).Select(EntityId).ToArrayAsync() |> Async.AwaitTask

            return eids, total
        }
