# Authorize_Should_AllowAnonymous_When_ReturnTypeIsProtected

## Result

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "parents",
        1,
        "protected"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "parents": [
      {
        "anonymous": {
          "id": 1
        },
        "protected": {
          "id": 1
        }
      },
      {
        "anonymous": {
          "id": 2
        },
        "protected": null
      },
      {
        "anonymous": {
          "id": 3
        },
        "protected": {
          "id": 3
        }
      }
    ]
  }
}
```

## Authorization calls

```json
[
  "BeforeResolver:parents[0].protected:<null>",
  "BeforeResolver:parents[1].protected:<null>",
  "BeforeResolver:parents[2].protected:<null>"
]
```

## Resolver batches

```json
[
  [
    1,
    3
  ],
  [
    1,
    2,
    3
  ]
]
```
