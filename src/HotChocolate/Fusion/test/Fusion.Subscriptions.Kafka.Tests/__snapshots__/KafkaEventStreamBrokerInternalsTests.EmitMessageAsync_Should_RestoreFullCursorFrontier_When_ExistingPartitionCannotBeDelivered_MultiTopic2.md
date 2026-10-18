# EmitMessageAsync_Should_RestoreFullCursorFrontier_When_ExistingPartitionCannotBeDelivered

## Write Outcome

```json
"Closed"
```

## Frontier Before

```json
[
  {
    "Topic": "topic-a",
    "Partition": 0,
    "NextOffset": 5
  },
  {
    "Topic": "topic-b",
    "Partition": 0,
    "NextOffset": 9
  }
]
```

## Frontier After

```json
[
  {
    "Topic": "topic-a",
    "Partition": 0,
    "NextOffset": 5
  },
  {
    "Topic": "topic-b",
    "Partition": 0,
    "NextOffset": 9
  }
]
```

## Delivered Message

```json
false
```
