# EmitMessageAsync_Should_PreserveFullCursorFrontier_When_MultiplePartitionsAreTracked

## Body

```text
{"id":1}
```

## Cursor

```text
AgAAAAEAAAAHdG9waWMtYQAAAAMAAAAAAAAABgAAAAAAAAACAAAAAAAAAAM=
```

## Raw Cursor Bytes

```text
020000000100000007746F7069632D6100000003000000000000000600000000000000020000000000000003
```

## Decoded Frontier

```json
[
  {
    "Topic": "topic-a",
    "Partition": 0,
    "NextOffset": 6
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

## Minted Partition Counts

```json
{
  "topic-a": 3
}
```

## Committed Frontier

```json
[
  {
    "Topic": "topic-a",
    "Partition": 0,
    "NextOffset": 6
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
