# Plan: WebSocket Batching — Server Support

## Context

The HTTP transport server already supports `OperationBatchRequest` and `VariableBatchRequest` through the execution engine. The WebSocket server (graphql-transport-ws protocol) currently rejects batch payloads in subscribe messages. This plan adds server-side support so batch requests sent over WebSocket are correctly parsed and executed.

---

## Step 1: Relax payload validation in `TryParseSubscribeMessage`

**File:** `src/HotChocolate/AspNetCore/src/AspNetCore.Pipeline/Subscriptions/Protocols/GraphQLOverWebSocket/GraphQLOverWebSocketProtocolHandler.cs`

Line 313-314 currently requires `payloadProp.ValueKind is not JsonValueKind.Object` — rejects arrays. Change to:

```csharp
payloadProp.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array)
```

`Utf8GraphQLRequestParser.Parse()` (already called on line 322) handles both objects and arrays natively.

---

## Step 2: Handle multi-request parse results

Same file, line 333 currently only uses `request[0]`. Change to:

- `requests.Length == 1` → existing path: `session.Operations.Enqueue(id, requests[0])`
- `requests.Length > 1` → new path: `session.Operations.EnqueueBatch(id, requests)`

**Variable batching needs no special handling:** payload `{"query":"...", "variables":[{...},{...}]}` is an object, parser returns `GraphQLRequest[1]` with array-valued variables, the existing single-request path handles it — the execution engine detects array variables and runs variable batching natively.

---

## Step 3: Add `EnqueueBatch` to OperationManager

**Files:**
- `src/HotChocolate/AspNetCore/src/AspNetCore.Pipeline/Subscriptions/IOperationManager.cs`
- `src/HotChocolate/AspNetCore/src/AspNetCore.Pipeline/Subscriptions/OperationManager.cs`

Add `bool EnqueueBatch(string sessionId, GraphQLRequest[] requests)` following the existing `Enqueue` pattern (lines 62-92) but calling `session.BeginExecuteBatch(requests, _cancellationToken)`.

---

## Step 4: Add `BeginExecuteBatch` to OperationSession

**Files:**
- `src/HotChocolate/AspNetCore/src/AspNetCore.Pipeline/Subscriptions/IOperationSession.cs`
- `src/HotChocolate/AspNetCore/src/AspNetCore.Pipeline/Subscriptions/OperationSession.cs`

Add `BeginExecuteBatch(GraphQLRequest[] requests, CancellationToken ct)` that:
1. Builds `IOperationRequest[]` from each `GraphQLRequest` via existing `CreateRequestBuilder` (line 132)
2. Creates `OperationRequestBatch(operationRequests)`
3. Calls `_executorSession.ExecuteBatchAsync(batch, ct)` (already exists on line 61-64)
4. Iterates `IResponseStream.ReadResultsAsync()`, sends each via existing `SendResultMessageAsync` (line 174)
5. Follows the exact error/completion/cleanup pattern of existing `SendResultsAsync` (lines 42-130)

Results already carry `RequestIndex` from the execution engine. `JsonResultFormatter.Format()` already writes `requestIndex`/`variableIndex` into the JSON payload.

---

## Files Modified

| File | Change |
|------|--------|
| `AspNetCore.Pipeline/.../GraphQLOverWebSocket/GraphQLOverWebSocketProtocolHandler.cs` | Relax payload validation, route multi-request to batch |
| `AspNetCore.Pipeline/Subscriptions/IOperationManager.cs` | Add `EnqueueBatch` |
| `AspNetCore.Pipeline/Subscriptions/OperationManager.cs` | Implement `EnqueueBatch` |
| `AspNetCore.Pipeline/Subscriptions/IOperationSession.cs` | Add `BeginExecuteBatch` |
| `AspNetCore.Pipeline/Subscriptions/OperationSession.cs` | Add `BeginExecuteBatch` + `SendBatchResultsAsync` |

## Reused (no changes needed)

- `Utf8GraphQLRequestParser.Parse()` — already handles arrays
- `ExecutorSession.ExecuteBatchAsync()` — already handles batch execution
- `JsonResultFormatter.Format()` — already writes `requestIndex`/`variableIndex`
- `OperationRequestBatch` — existing batch container type
- `OperationSession.CreateRequestBuilder()` — existing request builder helper

## Verification

1. `dotnet build src/HotChocolate/AspNetCore/src/AspNetCore.Pipeline/`
2. `dotnet test src/HotChocolate/AspNetCore/test/AspNetCore.Tests/` — existing tests pass
3. New tests: send batch subscribe message over WebSocket, verify multiple results with `requestIndex` come back
