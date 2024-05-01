# IsSelected_Pattern_Is_Invalid

## Result 1

```text
The specified pattern on field `BrokenQuery.user_1` is invalid:
`{
  email
  category {
    next {
      bar
    }
  }
}`

The field `bar` does not exist on type `Category`.
```

## Result 2

```text
The specified pattern on field `BrokenQuery.user_2` is invalid:
`{
  email
  category {
    ... on String {
      next {
        name
      }
    }
  }
}`

The type condition `String` of the inline fragment is not assignable from the parent field type `Category`.
```

