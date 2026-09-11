# DeferredBranch_Should_AwaitCleanup_When_BatchCleanupSuspends

## Context after cleanup suspension

```json
{
  "VariablesAlive": true,
  "ServicesAlive": true,
  "Argument": 1
}
```

## Payloads

```json
[
  "{\n  \"data\": {},\n  \"pending\": [\n    {\n      \"id\": \"2\",\n      \"path\": []\n    }\n  ],\n  \"hasNext\": true\n}",
  "{\n  \"incremental\": [\n    {\n      \"id\": \"2\",\n      \"data\": {\n        \"value\": 1\n      }\n    }\n  ],\n  \"completed\": [\n    {\n      \"id\": \"2\"\n    }\n  ],\n  \"hasNext\": false\n}"
]
```
