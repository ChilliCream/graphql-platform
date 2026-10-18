# AppendVary_Should_PreserveValuesAndAccessOrder_When_HeadersAreCustom

```json
[
  {
    "Name": "SingleMissing",
    "Vary": [
      "Accept",
      "X-foo"
    ],
    "Calls": [
      "get",
      "set"
    ]
  },
  {
    "Name": "MultipleMissing",
    "Vary": [
      "Accept",
      "X-foo, X-foo"
    ],
    "Calls": [
      "get",
      "set"
    ]
  },
  {
    "Name": "AllFound",
    "Vary": [
      "Accept"
    ],
    "Calls": [
      "get"
    ]
  },
  {
    "Name": "AllMissing",
    "Vary": [
      "Accept",
      " X-foo ,  X-bar "
    ],
    "Calls": [
      "get",
      "set"
    ]
  },
  {
    "Name": "Empty",
    "Vary": [
      "Accept"
    ],
    "Calls": [
      "get"
    ]
  },
  {
    "Name": "Wildcard",
    "Vary": [
      "Origin",
      "*"
    ],
    "Calls": [
      "get"
    ]
  },
  {
    "Name": "MultipleExistingValues",
    "Vary": [
      "Origin, Accept",
      "x-bar",
      "X-Foo"
    ],
    "Calls": [
      "get",
      "set"
    ]
  },
  {
    "Name": "RepeatedFoundNames",
    "Vary": [
      "Accept",
      "X-foo"
    ],
    "Calls": [
      "get",
      "set"
    ]
  },
  {
    "Name": "MissingWildcard",
    "Vary": [
      "Accept",
      "*"
    ],
    "Calls": [
      "get",
      "set"
    ]
  }
]
```
