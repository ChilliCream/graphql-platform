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
    "NextOffset": 1
  },
  {
    "Topic": "topic-a",
    "Partition": 1,
    "NextOffset": 2
  },
  {
    "Topic": "topic-a",
    "Partition": 2,
    "NextOffset": 3
  }
]
```

## Frontier After

```json
[
  {
    "Topic": "topic-a",
    "Partition": 0,
    "NextOffset": 1
  },
  {
    "Topic": "topic-a",
    "Partition": 1,
    "NextOffset": 2
  },
  {
    "Topic": "topic-a",
    "Partition": 2,
    "NextOffset": 3
  }
]
```

## Delivered Message

```json
false
```
