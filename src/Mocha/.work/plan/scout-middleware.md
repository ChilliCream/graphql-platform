# Middleware Pipeline & Concurrency Patterns Scout

## 1. Middleware Architecture Overview

### 1.1 Core Abstractions

**Location:** `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/Abstractions/`

#### Receive Middleware
- **ReceiveDelegate** (line 10): `delegate ValueTask ReceiveDelegate(IReceiveContext context)`
  - Core delegate for receive middleware pipeline execution
  - Each middleware step processes a single message through the pipeline
  
- **ReceiveMiddleware** (line 9): `delegate ReceiveDelegate ReceiveMiddleware(ReceiveMiddlewareFactoryContext context, ReceiveDelegate next)`
  - Factory delegate that creates middleware steps
  - Takes context and next handler, returns composed delegate
  
- **ReceiveMiddlewareConfiguration** (record at `/Mocha/Middlewares/Abstractions/ReceiveMiddlewareConfiguration.cs:8`):
  ```csharp
  public sealed record ReceiveMiddlewareConfiguration(ReceiveMiddleware Middleware, string Key);
  ```
  - Pairs middleware factory with string key for pipeline identification/ordering
  
- **ReceiveMiddlewareFactoryContext** (`/Middlewares/Abstractions/ReceiveMiddlewareFactoryContext.cs`):
  - Contains: Services, Endpoint, Transport
  - Provides access to dependencies, endpoint-level features, transport-level features

#### Dispatch Middleware (symmetrical pattern)
- **DispatchDelegate**: `delegate ValueTask DispatchDelegate(IDispatchContext context)`
- **DispatchMiddleware**: `delegate DispatchDelegate DispatchMiddleware(DispatchMiddlewareFactoryContext context, DispatchDelegate next)`
- **DispatchMiddlewareConfiguration**: Pairs factory with key

#### Consumer (Handler) Middleware
- **ConsumerDelegate**, **ConsumerMiddleware**, **ConsumerMiddlewareConfiguration**
- Same pattern for handlers executing received messages

### 1.2 Middleware Compilation

**Location:** `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/MiddlewareCompiler.cs`

**Key Points:**
- Three static compile methods: `CompileDispatch`, `CompileReceive`, `CompileHandler`
- Middleware lists are **reversed before folding** (lines 45-46, 86, 129)
  - Registration order remains intuitive
  - Execution follows nested middleware pattern (outermost first)
- **Reusable list pooling** for allocation efficiency (Interlocked.Exchange, lines 29, 70, 110)
- Pipeline modifiers can reorder/replace middleware at build time (lines 37-42)
- Fold right-to-left composition (lines 50-54): each middleware wraps the next

**Compilation Flow:**
1. Gather middleware from endpoint → transport → bus configurations (lines 32-35)
2. Apply pipeline modifiers (lines 37-42)
3. Reverse list to make registration order intuitive (line 46)
4. Fold right-to-left, building nested delegate chain (lines 50-54)
5. Return fully composed pipeline

### 1.3 Default Receive Middleware Stack

**Location:** `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/Receive/ReceiveMiddlewares.cs`

Order (as registered, execution is reversed):
1. **TransportCircuitBreaker** (line 13): Transport-level breaker, stops consuming if broker fails
2. **ConcurrencyLimiter** (line 19): Throttles concurrent message processing
3. **Instrumentation** (line 24): Emits telemetry/tracing
4. **CircuitBreaker** (line 29): Stops processing after repeated failures in handlers
5. **DeadLetter** (line 34): Routes unprocessable messages to DLQ
6. **Fault** (line 39): Converts exceptions to explicit fault signals
7. **Expiry** (line 44): Discards messages past TTL
8. **MessageTypeSelection** (line 50): Resolves CLR type from envelope
9. **Routing** (line 55): Dispatches to appropriate consumer

## 2. Concurrency Patterns

### 2.1 ConcurrencyLimiter Middleware

**Location:** `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/Receive/ConcurrencyLimiter/`

**ConcurrencyLimiterMiddleware** (`ConcurrencyLimiterMiddleware.cs:10`):
```csharp
public sealed class ConcurrencyLimiterMiddleware(int maxConcurrency) : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(maxConcurrency, maxConcurrency);
    
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        await _semaphore.WaitAsync(context.CancellationToken);
        try
        {
            await next(context);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

**Key Details:**
- Uses SemaphoreSlim to limit concurrent message processing
- Semaphore acquired before pipeline execution, released in finally block
- Configuration resolved from endpoint → transport → bus (feature cascading, lines 38-73)
- Can be disabled via Enabled feature flag
- **Does not handle retry** - just gates concurrency

### 2.2 InMemory Transport - ChannelProcessor Pattern

**Location:** `/workspaces/hc3/src/Mocha/src/Mocha.Transport.InMemory/InMemoryReceiveEndpoint.cs`

**Concurrent Consumer Architecture:**
```csharp
protected override ValueTask OnStartAsync(IMessagingRuntimeContext context, CancellationToken cancellationToken)
{
    var logger = context.Services.GetRequiredService<ILogger<InMemoryReceiveEndpoint>>();
    
    _processor = new ChannelProcessor<InMemoryQueueItem>(
        Queue.ConsumeAsync,           // Source factory
        (item, ct) => ProcessMessageAsync(item, logger, ct),  // Handler
        _maxDegreeOfParallelism);     // Concurrency count
    
    return ValueTask.CompletedTask;
}
```

**ChannelProcessor<T>** (`/workspaces/hc3/src/Mocha/src/Mocha.Threading/ChannelProcessor.cs`):
- Creates N concurrent `ContinuousTask` workers (lines 34-44)
- Each worker calls source factory independently → gets IAsyncEnumerable<T>
- Runs handler for each item
- **Source must support concurrent readers** (e.g., ChannelReader, InMemoryQueue)
- Disposed via CancellationTokenSource (lines 50-67)

**ContinuousTask** (`/workspaces/hc3/src/Mocha/src/Mocha.Threading/ContinuousTask.cs`):
- Background loop that runs delegate repeatedly (RunContinuously method, lines 49-75)
- **Exponential backoff on failure** (lines 18-21: 100ms base, 10s cap)
- Never-ending loop with `await Task.Yield()` to prevent tight spinning (line 61)
- Graceful disposal via CancellationToken (lines 78-109)

**MaxConcurrency Configuration:**
- InMemoryReceiveEndpoint: `_maxDegreeOfParallelism = configuration.MaxConcurrency ?? ReceiveEndpointConfiguration.Defaults.MaxConcurrency` (line 37-38)
- Default: `Environment.ProcessorCount * 2` (`/Endpoints/Configurations/ReceiveEndpointConfiguration.cs:63`)
- Can be overridden per endpoint via `.MaxConcurrency(int)` fluent API

### 2.3 Kafka Transport - Single Consumer Loop

**Location:** `/workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/KafkaReceiveEndpoint.cs`

**Current Architecture:**
```csharp
private async Task ConsumeLoopAsync(
    IConsumer<byte[], byte[]> consumer,
    CancellationToken cancellationToken)
{
    try
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumeResult<byte[], byte[]>? result;
            try
            {
                result = consumer.Consume(cancellationToken);  // Blocking poll
            }
            catch (ConsumeException ex)
            {
                _logger.KafkaConsumeError(ConsumerGroupId, ex.Error.Reason);
                continue;
            }
            
            if (result is null)
            {
                continue;
            }
            
            try
            {
                await ExecuteAsync(
                    static (context, state) =>
                    {
                        var feature = context.Features.GetOrSet<KafkaReceiveFeature>();
                        feature.ConsumeResult = state.result;
                        feature.Consumer = state.consumer;
                    },
                    (result, consumer),
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.KafkaConsumeLoopError(ConsumerGroupId, ex);
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Expected on shutdown
    }
}
```

**Constraints:**
- **Single-threaded consume loop** (lines 85-115)
- Calls `consumer.Consume(cancellationToken)` sequentially (line 90)
- Awaits full receive pipeline for each message (line 106)
- ConcurrencyLimiter middleware gates actual concurrent processing (not consumers)
- Offset commits implicitly handled by Confluent client (auto-commit or manual)
- **Partition affinity preserved** - single consumer/partition relationship in Confluent model

**KafkaReceiveFeature** (`/Features/KafkaReceiveFeature.cs`):
- Carries ConsumeResult and Consumer reference through pipeline (lines 15, 20)
- Provides access to Topic, Partition, Offset (lines 25-35)

## 3. Error Handling & Resilience

### 3.1 ReceiveFaultMiddleware

**Location:** `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/Receive/ReceiveFaultMiddleware.cs`

**Two-Path Failure Contract:**
1. **Request/Response**: Send NotAcknowledgedEvent to ResponseAddress (lines 41-45)
2. **Fire-and-forget**: Forward to ErrorEndpoint with FaultInfo in headers (lines 47-49)

**Fault Creation** (line 38):
```csharp
var fault = FaultInfo.From(Guid.NewGuid(), provider.GetUtcNow(), ex);
```

**Error Forwarding** (lines 104-134):
- Sends message + exception metadata to configurable error endpoint
- Preserves correlation: CorrelationId, ConversationId
- Headers include: ExceptionType, Message, StackTrace, Timestamp (lines 155-167)

### 3.2 CircuitBreakerMiddleware

**Location:** `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/Receive/CircuitBreaker/`

**Implementation:**
- Uses **Polly** resilience library (lines 3-4)
- Configurable via CircuitBreakerFeature (lines 44-86)
- Parameters: FailureRatio, MinimumThroughput, SamplingDuration, BreakDuration
- Default: 50% failure ratio, 10 min throughput, 10s sampling, 30s break (CircuitBreakerOptions.Defaults)

**Retry Loop on BrokenCircuitException** (lines 19-40):
```csharp
while (true)
{
    try
    {
        await resiliencePipeline.ExecuteAsync(...);
        return;
    }
    catch (BrokenCircuitException ex)
    {
        var totalMilliseconds = (long)(ex.RetryAfter?.TotalMilliseconds ?? breakDuration.TotalMilliseconds);
        // Clamp between 0 and uint.MaxValue
        await Task.Delay(TimeSpan.FromMilliseconds(totalMilliseconds), timeProvider, context.CancellationToken);
    }
}
```

**Key Detail:** Exponential backoff hints from Polly are clamped to sane bounds (0 to uint.MaxValue)

### 3.3 TransportCircuitBreakerMiddleware

**Location:** `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/Receive/TransportCircuitBreaker/`

- Transport-level breaker (stops entire receive endpoint)
- Separate from handler circuit breaker
- Prevents cascading failures when broker is unavailable

## 4. Batch Consumer Concurrency

**Location:** `/workspaces/hc3/src/Mocha/src/Mocha/Consumers/Implementations/BatchConsumer.cs`

**Channel-Based Pattern:**
```csharp
_channel = Channel.CreateBounded<MessageBatch<TEvent>>(
    new BoundedChannelOptions(options.MaxConcurrentBatches)
    {
        SingleReader = options.MaxConcurrentBatches == 1
    });

_processor = new ChannelProcessor<MessageBatch<TEvent>>(
    _channel.Reader.ReadAllAsync,
    ProcessBatchAsync,
    options.MaxConcurrentBatches);  // Concurrency count
```

**Key Points:**
- Uses BoundedChannel with configurable max concurrent batches (lines 46-50)
- ChannelProcessor runs N workers reading from channel (line 52-55)
- SingleReader optimization for maxConcurrentBatches == 1 (line 49)
- Messages held in batch until handler completes (lines 60-72)
- Preserves middleware semantics (ACK, fault, circuit breaker per message) without modification

## 5. Feature Configuration Pattern

**Hierarchical Resolution** (common pattern in all middleware):
```csharp
var value = context.Endpoint.Features.GetFeatureValue(selector)
    ?? context.Transport.Features.GetFeatureValue(selector)
    ?? busFeatures.GetFeatureValue(selector);
```

**Example from ConcurrencyLimiterMiddleware** (lines 65-74):
- Most specific scope (endpoint) takes precedence
- Falls back to transport scope
- Falls back to bus scope
- Enables override cascading: configure at bus level, override per endpoint

**Applied To:**
- ConcurrencyLimiterFeature: MaxConcurrency, Enabled
- CircuitBreakerFeature: FailureRatio, MinimumThroughput, SamplingDuration, BreakDuration, Enabled
- Custom features can follow same pattern

## 6. Receive Endpoint Lifecycle

**Location:** `/workspaces/hc3/src/Mocha/src/Mocha/Endpoints/ReceiveEndpoint.cs`

**Strict Sequence:**
1. **Initialize** (`OnInitialize`): Parse configuration, validate required fields
2. **DiscoverTopology**: Resolve topology resources (queues, topics)
3. **Complete** (`OnComplete`): Compile receive middleware pipeline, build features
4. **StartAsync** (`OnStartAsync`): Start transport-specific consume loops, create workers
5. **StopAsync** (`OnStopAsync`): Signal cancellation, dispose workers/consumers

**Pipeline Compilation During Complete:**
- Gathers middleware from endpoint, transport, bus configurations
- Compiles via MiddlewareCompiler.CompileReceive (lines 63-101 in MiddlewareCompiler.cs)
- Result: single ReceiveDelegate pipeline
- Stored as _pipeline (line 24 in ReceiveEndpoint.cs)

**Message Processing:**
- ExecuteAsync method (line 100+ in ReceiveEndpoint.cs) invokes compiled _pipeline
- Runs inside scoped IServiceProvider
- ReceiveContext pooled for allocation efficiency

## 7. Runtime Startup/Shutdown

**Location:** `/workspaces/hc3/src/Mocha/src/Mocha/Runtime/MessagingRuntime.cs`

**StartAsync** (lines 116-129):
```csharp
public async ValueTask StartAsync(CancellationToken cancellationToken)
{
    if (IsStarted)
    {
        return;
    }
    
    foreach (var transport in transports)
    {
        await transport.StartAsync(this, cancellationToken);
    }
    
    IsStarted = true;
}
```

- Starts each transport sequentially
- Transports start their receive endpoints
- Receive endpoints start their consume loops/workers

**GetTransport** (lines 102-105):
```csharp
public MessagingTransport? GetTransport(Uri address)
{
    return transports.FirstOrDefault(t => t.Schema == address.Scheme);
}
```

- Routes by URI scheme (kafka://, amqp://, etc.)

## 8. Existing Retry-Like Behavior

### In ContinuousTask (Exponential Backoff)
- Lines 18-21: Backoff configuration (100ms base, 10s cap)
- Lines 49-75: RunContinuously loop with exponential backoff on unhandled exceptions
- No explicit retry count, infinite retries with backoff

### In CircuitBreakerMiddleware
- Polly circuit breaker with retry hint (`RetryAfter` property)
- Infinite retry loop awaiting delay between attempts (lines 19-40)
- No explicit retry limit per message, circuit opens after threshold

### Batch Retry Exceeded
- **BatchRetryExceededException** (`/Consumers/Batching/BatchRetryExceededException.cs`)
- Thrown when batch exceeds max retry limit
- **Does not exist yet** - only exception class defined, no retry logic found

### No General-Purpose Retry Middleware Found
- No dedicated RetryMiddleware found
- No Polly Retry strategy applied to receive pipeline
- Resilience via circuit breaker + exponential backoff in ContinuousTask
- **Gap**: No configurable retry count, backoff strategy for message processing failures

## 9. Constraints for Kafka Concurrent Consumers

### Partition Affinity
- Confluent.Kafka client maintains consumer group coordination
- Each partition assigned to one consumer in group
- Single IConsumer<byte[], byte[]> instance per endpoint
- Cannot split partition across multiple consumers safely

### Offset Management
- Current implementation: implicit auto-commit by librdkafka config
- Single consumer loop sequences offset commits with message processing
- Multiple concurrent consumers on same partition would race on offsets
- **Solution constraint**: Concurrency must be handled at pipeline level (via ConcurrencyLimiter middleware + ChannelProcessor pattern from InMemory), not consumer level

### Rebalancing
- Consumer group rebalancing triggered by partition reassignment
- Single endpoint consumer loop must handle rebalance gracefully
- Multiple consumer instances can be used (different endpoints, different consumer groups)

### Current Config in KafkaReceiveEndpoint
- ConsumerGroupId: defaults to TopicName (line 41)
- Topic: resolved from topology (lines 50-51)
- Consumer: single instance created per endpoint (line 67)
- Consume loop: single background task (lines 70-74)

## 10. Configuration API Patterns

### Kafka Descriptor
**Location:** `/workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/Descriptors/IKafkaReceiveEndpointDescriptor.cs`

```csharp
public interface IKafkaReceiveEndpointDescriptor : IReceiveEndpointDescriptor<KafkaReceiveEndpointConfiguration>
{
    new IKafkaReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);
    new IKafkaReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;
    new IKafkaReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);
    new IKafkaReceiveEndpointDescriptor FaultEndpoint(string name);
    new IKafkaReceiveEndpointDescriptor SkippedEndpoint(string name);
    IKafkaReceiveEndpointDescriptor Topic(string name);
    IKafkaReceiveEndpointDescriptor ConsumerGroup(string groupId);
    new IKafkaReceiveEndpointDescriptor UseReceive(ReceiveMiddlewareConfiguration configuration, string? before = null, string? after = null);
}
```

### Configuration Cascading
- Endpoint-level config overrides transport-level
- Transport-level config overrides bus-level
- Feature-based, not inheritance-based

## 11. Summary: Retry vs Concurrent Implementation Paths

### For Retry Middleware (#3):
1. Create ReceiveMiddleware that wraps next with retry policy
2. Use Polly Retry strategy or custom backoff
3. Store attempt count in ReceiveContext or feature
4. Handle MaxRetries exceeded → forward to DLQ or error endpoint
5. Integrate into ReceiveMiddlewares defaults or optional configuration

### For Concurrent Consumers (#4) on Kafka:
1. **Cannot use multiple IConsumer instances on same partition** (Kafka model constraint)
2. **Must implement ChannelProcessor pattern** (like InMemory):
   - Decouple consume loop from message processing
   - Consume loop reads sequentially from Kafka (single consumer, offset ordering preserved)
   - ChannelProcessor with N workers processes messages concurrently
   - Concurrency controlled by MaxConcurrency configuration
   - ConcurrencyLimiter middleware can further gate pipeline processing
3. **Use BoundedChannel** to queue messages between consume loop and processor workers
4. **Preserve offset ordering** by committing after ProcessBatch completes
5. **Handle rebalancing** during channel handoff
6. **Configuration**: MaxConcurrency per endpoint, default: ProcessorCount * 2

---

**Generated:** 2026-04-09
**Scout Depth:** Very thorough - all middleware files, core abstractions, concurrency patterns, InMemory/Kafka transports, feature configuration, lifecycle, resilience patterns
