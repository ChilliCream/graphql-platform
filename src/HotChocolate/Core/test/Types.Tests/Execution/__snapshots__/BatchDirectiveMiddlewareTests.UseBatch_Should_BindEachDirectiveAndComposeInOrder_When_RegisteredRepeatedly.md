# UseBatch_Should_BindEachDirectiveAndComposeInOrder_When_RegisteredRepeatedly

## First field

```json
{
  "data": {
    "first": "A(A2(B(B2(field(format(value))))))"
  }
}
```

## Second field

```json
{
  "data": {
    "second": "C(C2(field(format(value))))"
  }
}
```

## Middleware order

```json
[
  "A:before",
  "A2:before",
  "B:before",
  "B2:before",
  "field:before",
  "field:after",
  "B2:after",
  "B:after",
  "A2:after",
  "A:after",
  "C:before",
  "C2:before",
  "field:before",
  "field:after",
  "C2:after",
  "C:after"
]
```
