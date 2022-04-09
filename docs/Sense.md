## Syntax

### Value

```
value
```

```
some-value
```

```
"complex value"
```

### List

```
[ item1 item2 item3 ]
```

```
[
    value1
    [ value2 value3 ]
    "complex value #4"
]
```

### Map

Key must be a value (not list or map)

```
{ key value }
```

```
{
    key1 value1
    key2 value2
}
```

```
{ key1 value1 key2 value2 }
```

```
{
    key1 {
        inner-key1 inner-value1
        inner-key2 inner-value2
    }
    key2 value2
}
```

### Samples

Some examples:

```
{
    file {
        mime "image/png"
        size 20000B
    }
    tags [
        nature
        sky
    ]
}
```

> Same but one-liner
> ```
> { file { mime "image/png" size 20000B } tags [ nature sky ] }
> ```
