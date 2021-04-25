## `mind`
```
add: EntityId * Sense -> IO ()
remove: EntityId -> IO ()

query: Query -> EntityId[]
```

## `storage`
```
read: EntityId -> IO Data
write: EntityId * Data -> IO ()
delete: EntityId -> IO ()

{link: EntityId * Target -> IO ()}
```
