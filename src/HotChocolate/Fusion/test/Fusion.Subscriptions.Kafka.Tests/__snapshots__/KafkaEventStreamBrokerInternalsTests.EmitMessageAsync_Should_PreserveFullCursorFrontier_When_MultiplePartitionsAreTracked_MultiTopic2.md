# EmitMessageAsync_Should_PreserveFullCursorFrontier_When_MultiplePartitionsAreTracked

## Body

```text
{"id":1}
```

## Cursor

```text
AgAAAAIAAAAHdG9waWMtYQAAAAEAAAAAAAAABgAAAAd0b3BpYy1iAAAAAQAAAAAAAAAJ
```

## Raw Cursor Bytes

```text
020000000200000007746F7069632D6100000001000000000000000600000007746F7069632D62000000010000000000000009
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
    "Topic": "topic-b",
    "Partition": 0,
    "NextOffset": 9
  }
]
```

## Minted Partition Counts

```json
{
  "topic-a": 1,
  "topic-b": 1
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
    "Topic": "topic-b",
    "Partition": 0,
    "NextOffset": 9
  }
]
```
