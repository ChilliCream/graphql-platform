# AddPartialResults_Should_SkipNulledPositions_When_PropagatedNullPrecedesLaterListResult

## Data

```json
{"users":[null,{"id":"2","email":"b"}]}
```

## Error Paths

```json
[
  "users[0].email"
]
```
