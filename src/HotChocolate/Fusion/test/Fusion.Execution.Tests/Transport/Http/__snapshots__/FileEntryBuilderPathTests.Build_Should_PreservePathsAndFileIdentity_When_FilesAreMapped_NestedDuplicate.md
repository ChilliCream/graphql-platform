# Build_Should_PreservePathsAndFileIdentity_When_FilesAreMapped

## Cleaned Variables

```json
{"input":{"files":[null,null,null]},"text":"kept"}
```

## File Map

```json
[
  {
    "Key": "1",
    "Path": "variables.input.files.0",
    "SameFile": true
  },
  {
    "Key": "2",
    "Path": "variables.input.files.1",
    "SameFile": true
  },
  {
    "Key": "1",
    "Path": "variables.input.files.2",
    "SameFile": true
  }
]
```

## Lookup Order

```json
[
  "1",
  "2",
  "1"
]
```
