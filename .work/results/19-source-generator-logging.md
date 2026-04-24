# Source-generator-based logging in `Mocha.Transport.AzureServiceBus`

## TL;DR

- **Recommendation:** Yes, convert. The codebase has a clear precedent (RabbitMQ transport), the rule `CA1848` flags the current calls, and the `[LoggerMessage]` source generator is the documented "modern .NET" idiom.
- **Scope is small.** There are exactly **two** `ILogger` call sites in the entire ASB transport directory. Both are in `AzureServiceBusReceiveEndpoint.cs` inside the `ProcessErrorAsync` handler. Convert both.
- **Pattern to follow:** Co-locate an `internal static partial class Logs` at the bottom of the same `.cs` file as the consuming class (RabbitMQ convention), with `[LoggerMessage(LogLevel.X, "...")]` extension methods on `ILogger`. Two methods (one Warning, one Error) — **not** a single method with a `LogLevel` parameter — because the two events are semantically distinct and we can give each its own `EventId`.

---

## 1. Where the snippet lives — full context

File: `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusReceiveEndpoint.cs`

The handler is wired up in `OnStartAsync` (lines 78–127):

- Line 78 resolves the logger: `var logger = context.Services.GetRequiredService<ILogger<AzureServiceBusReceiveEndpoint>>();`
- The logger is captured by the `ProcessErrorAsync` lambda (line 104).
- Inside the lambda (lines 107–124) the snippet branches on `IsTransientProcessorError(args.Exception)`:
  - Transient -> `logger.LogWarning(ex, "Azure Service Bus processor transient error on entity {EntityPath} (Source: {ErrorSource})", ...)`
  - Otherwise -> `logger.LogError(ex, "Azure Service Bus processor error on entity {EntityPath} (Source: {ErrorSource})", ...)`
- `IsTransientProcessorError` (lines 145–163) returns true for `OperationCanceledException` and `ServiceBusFailureReason.{ServiceCommunicationProblem,ServiceBusy,ServiceTimeout,MessageLockLost,SessionLockLost}`.

Hot path? **Yes, in the unhappy case.** The Azure Service Bus SDK can fan out `ProcessErrorAsync` callbacks rapidly when a connection drops, when locks expire under load, or during throttling — exactly the scenarios where boxing the `args.ErrorSource` enum (a `ServiceBusErrorSource` value type) and re-parsing the format string per call hurts. This is precisely what the source generator was built to eliminate.

## 2. Existing `[LoggerMessage]` patterns in the ASB transport

**There are none.** Search for `LoggerMessage` under `src/Mocha/src/Mocha.Transport.AzureServiceBus/` returns zero hits. There is no `Logs.cs`, `Telemetry.cs`, or `AzureServiceBusLog.cs`. We will be establishing the pattern here for the first time in this transport.

## 3. Sibling-transport conventions

### RabbitMQ — strong precedent, follow this

Two files in `src/Mocha/src/Mocha.Transport.RabbitMq/Connection/` use the source generator:

- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMq/Connection/RabbitMQDispatcher.cs` — 6 entries (lines 140–159).
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMq/Connection/RabbitMQConsumerManager.cs` — ~30 entries (lines 385–525).

The convention, distilled:

1. **Co-located.** Each consumer file appends its own `internal static partial class Logs` block at the bottom of the file. Both blocks live in the same namespace (`Mocha.Transport.RabbitMQ`); because both are declared `partial`, the C# compiler merges them into one class — the source generator handles each declaration independently.
2. **Visibility:** `internal static partial class Logs`.
3. **Extension methods on `ILogger`:** `public static partial void XxxYyy(this ILogger logger, ...);` — call sites read `Logger.XxxYyy(...)` (no boxing of value-type args, no template re-parse).
4. **Attribute form:** positional `[LoggerMessage(LogLevel.X, "Message with {Placeholder}")]`. **No explicit `EventId`** is set in the RabbitMQ files — the generator auto-assigns. This matches existing convention; we'll follow it. (If we want stable IDs across releases for log-analytics correlation, that is a separate decision; the existing code does not invest in stable IDs.)
5. **Method naming:** PascalCase verb-phrase mirroring the message ("FailedToProvisionTopology", "RentedChannelFromPool", "ReconnectionAttemptFailed"). Past-tense for events that happened, imperative for actions in progress.
6. **Exception param:** placed as the parameter immediately after `this ILogger logger`, named `ex`. The source generator recognises it as the exception slot and does **not** require a `{Exception}` template placeholder.
7. **Placeholder casing:** PascalCase placeholders — `{Queue}`, `{Exchange}`, `{ConsumerTag}`, `{Attempts}`. Matches the Microsoft Learn guidance.

### InMemory — does not use it

`src/Mocha/src/Mocha.Transport.InMemory/InMemoryReceiveEndpoint.cs` line 83 uses `logger.LogCritical(ex, "Error processing message")` directly. Single call site, never converted. Not a counter-example — just hasn't been touched.

### Wider Mocha codebase

`Grep "LoggerMessage"` across `src/Mocha/` returns **20 files** that already use the source generator (Postgres transport, EF Core scheduling/outbox, Saga, BatchConsumer, Inbox, etc.). The pattern is firmly established as the house style; the ASB transport is the outlier.

## 4. `LoggerMessage` source generator — what matters here

From Microsoft Learn (`learn.microsoft.com/dotnet/core/extensions/logging/source-generation` and the high-performance-logging article):

- **Performance:** eliminates boxing of value types, parses the message template once at compile time, no temporary allocations in the disabled-level fast path. This is exactly why `CA1848` ("Use the LoggerMessage delegates") fires on the current code.
- **Constraints (relevant ones):**
  - Method must be `partial` and return `void`.
  - Method names cannot start with `_`.
  - For `static` methods, `ILogger` must be a parameter; with `this`, becomes an extension method (RabbitMQ style).
  - One `Exception` parameter is recognised as the implicit exception — no `{Exception}` placeholder required, the logging pipeline attaches it to the log entry.
- **`Level` choice — three forms:**
  1. **`Level` in the attribute** (most common). One method per level.
  2. **`Level` omitted from attribute, `LogLevel` parameter on the method.** Source generator picks up the runtime value; SYSLIB1017 enforces that you set it somewhere.
  3. Two attribute overloads exist: `[LoggerMessage(LogLevel level, string message)]` (no EventId) and `[LoggerMessage(int eventId, LogLevel level, string message)]` (with EventId).
- **`EventId`:** optional. Auto-assigned by the generator when omitted. The RabbitMQ code in this repo omits it.

### Two methods vs. one with `LogLevel` parameter

Both are valid. For this site, **two methods is correct**:

- The two log entries are **semantically distinct events** (transient vs. unknown-fault). They deserve their own event names so structured-log consumers can filter on `EventName`.
- They use different message templates ("transient error" vs. "error"). Form 2 (single method, `LogLevel` parameter) requires identical templates by design.
- Two methods is also what RabbitMQ does throughout (no use of dynamic level), so it stays consistent.

## 5. Proposed refactor — partial-class skeleton

Append a `Logs` block at the bottom of `AzureServiceBusReceiveEndpoint.cs` (after line 164), and rewrite the lambda body. No other files change.

```csharp
// Inside ProcessErrorAsync lambda (lines 107-124 today):
_processor.ProcessErrorAsync += args =>
{
    // Transient/recoverable conditions are surfaced as warnings; only unknown faults escalate to error.
    if (IsTransientProcessorError(args.Exception))
    {
        logger.ProcessorTransientError(args.Exception, args.EntityPath, args.ErrorSource);
    }
    else
    {
        logger.ProcessorError(args.Exception, args.EntityPath, args.ErrorSource);
    }

    return Task.CompletedTask;
};
```

```csharp
// Appended at the bottom of AzureServiceBusReceiveEndpoint.cs,
// inside the same `Mocha.Transport.AzureServiceBus` namespace.

internal static partial class Logs
{
    [LoggerMessage(
        LogLevel.Warning,
        "Azure Service Bus processor transient error on entity {EntityPath} (Source: {ErrorSource})")]
    public static partial void ProcessorTransientError(
        this ILogger logger,
        Exception ex,
        string entityPath,
        ServiceBusErrorSource errorSource);

    [LoggerMessage(
        LogLevel.Error,
        "Azure Service Bus processor error on entity {EntityPath} (Source: {ErrorSource})")]
    public static partial void ProcessorError(
        this ILogger logger,
        Exception ex,
        string entityPath,
        ServiceBusErrorSource errorSource);
}
```

Notes:

- `ServiceBusErrorSource` is the enum on `ProcessErrorEventArgs.ErrorSource` (from `Azure.Messaging.ServiceBus`, already imported at the top of the file). Passing it as a strongly-typed parameter avoids the boxing the current `LogWarning`/`LogError` calls perform.
- `args.EntityPath` is `string`; pass directly.
- No explicit `EventId` — matches RabbitMQ convention; the generator assigns deterministic IDs per file.
- Method names mirror the RabbitMQ verb-phrase style (`Processor*`, paralleling `Connection*`, `Channel*`).
- The `IsTransientProcessorError` helper stays. It is still the right place for the classification logic.

## 6. Full call-site inventory in the ASB transport

`Grep` across `src/Mocha/src/Mocha.Transport.AzureServiceBus/` for `_logger.|logger.Log[A-Z]|ILogger`:

| File | Line | Kind | Notes |
| --- | --- | --- | --- |
| `AzureServiceBusReceiveEndpoint.cs` | 78 | `GetRequiredService<ILogger<AzureServiceBusReceiveEndpoint>>` | Resolution only; not a log call. |
| `AzureServiceBusReceiveEndpoint.cs` | 109 | `logger.LogWarning(ex, "Azure Service Bus processor transient error on entity {EntityPath} (Source: {ErrorSource})", args.EntityPath, args.ErrorSource)` | **Convert.** Hot path on transient errors. |
| `AzureServiceBusReceiveEndpoint.cs` | 118 | `logger.LogError(ex, "Azure Service Bus processor error on entity {EntityPath} (Source: {ErrorSource})", args.EntityPath, args.ErrorSource)` | **Convert.** |

That is the entire inventory. Searched directories with **zero** logger usage:

- `Configurations/` (7 files)
- `Connection/` — only `AzureServiceBusClientManager.cs`; no logging.
- `Conventions/` (6 files)
- `Descriptors/` (6 files)
- `Features/` (3 files)
- `Middlewares/Receive/` — `AzureServiceBusAcknowledgementMiddleware.cs`, `AzureServiceBusParsingMiddleware.cs`, `AzureServiceBusReceiveMiddlewares.cs`. No logging despite handling settle/parse failures (today they just throw or swallow — see `01-message-lock-lost-exception.md` and `03-entity-terminology.md`). If we ever add logging in the middleware (e.g., to record swallowed `MessageLockLost` events for diagnostics), it should follow the same `Logs` pattern from day one.
- `Scheduling/AzureServiceBusScheduledMessageStore.cs` — no logger; `CancelAsync` silently returns `false` on `MessageNotFound`. Same recommendation: if observability is added later, do it via `[LoggerMessage]`.
- `Topology/` (and its sub-folders) — no logging.
- `AzureServiceBusDispatchEndpoint.cs`, `AzureServiceBusMessageEnvelopeParser.cs`, `AzureServiceBusMessageHeaders.cs`, `AzureServiceBusMessagingTransport.cs`, `MessageBusBuilderExtensions.cs`, `Assembly.cs` — no logging.

### Recommendation on scope

**Convert both call sites now.** Do not invent new log events for the no-log files just to "match" RabbitMQ — that would violate "Minimal Impact". The pattern is tiny (one `Logs` block, two methods) and self-contained in the one file that already needs the change. Future additions in the middlewares/scheduler/dispatcher should follow the same pattern from the start; this PR establishes the convention so subsequent files have something to copy.

## 7. Sanity checks before opening the PR

- `using Microsoft.Extensions.Logging;` is already present at the top of `AzureServiceBusReceiveEndpoint.cs` (line 3) — no new `using`.
- `using Azure.Messaging.ServiceBus;` is already present (line 1) — `ServiceBusErrorSource` resolves without an import change.
- The project (`Mocha.Transport.AzureServiceBus.csproj`) targets the repo-wide `<TargetFramework>` (set via `Directory.Build.props`). The source generator ships with `Microsoft.Extensions.Logging.Abstractions`, transitively referenced via `Mocha`/`Mocha.Abstractions`. No new `<PackageReference>` is needed — the RabbitMQ project uses the same chain and compiles cleanly.
- `IsTransientProcessorError` stays `private static`. Unchanged.
- Build verification: `dotnet build src/Mocha/src/Mocha.Transport.AzureServiceBus/Mocha.Transport.AzureServiceBus.csproj`. Source-generator failures surface as `SYSLIB10xx` diagnostics — most likely none, but watch for `SYSLIB1006` (duplicate event IDs — n/a since we omit them) and `SYSLIB1015` (unused parameter — n/a since both placeholders are referenced).
