## Символ

Символ — это именованный набор бинарных отношений с этим же символом.

$$
A = \langle Name(A), Rel(A) \rangle
$$

Любой символ существует в неявном виде, не имея никаких связей.

## Свойтсва

```
the_picture {
    width := 720
}

the_picture.width := 720

# the_picture <- width := 720

human {
    _0 := 2 * arm
    2 * leg
}
```

----

## Подобие

Если символ $A$ подобен символу $B$, то все связи символа $B$ относяться к $A$.

$$
A \sim B \Rarr Rel(B)[B := A] \subseteq Rel(A)
$$

```
human <- 2 * leg
human <- 2 * arm

woman ~ human

=>

woman <- 2 * leg
woman <- 2 * arm
```

----

```
0 ~ integer
1 ~ integer
2 ~ integer

((x ~ integer) < (y ~ integer))
= x != y
| x:=0 & y:=1
| x:=0 & y:=2
| ...
| x:=1 & y:=2
| x:=1 & y:=3
| ...


```
