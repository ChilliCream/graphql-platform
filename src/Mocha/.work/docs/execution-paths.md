# Mocha Kafka Transport: End-to-End Execution Paths

This document traces the complete flow of operations through the Mocha Kafka transport architecture, documenting key classes, methods, decision points, and operational semantics.

---

## 1. Publish Flow

**Pattern**: One-to-many fanout to all subscribed consumers on a topic

### Entry Point: `IMessageBus.PublishAsync<T>(message, options?, cancellationToken)`

**Location**: `/src/Mocha/Middlewares/DefaultMessageBus.cs:38-76`

1. **Message Routing** (DefaultMessageBus.PublishAsync)
   - Obtains `MessageType` via `runtime.GetMessageType(message.GetType())`
   - Resolves publish endpoint: `endpoint = runtime.GetPublishEndpoint(messageType)`
   - The `MessageRouter` maps the message type to all transports that have inbound routes for this type on their publish endpoint
   
2. **Context Setup** (DefaultMessageBus.PublishAsync)
   - Pools a `DispatchContext` from `_contextPool`
   - Initializes context with:
     - Services provider (DI scope)
     - Resolved endpoint
     - Runtime instance
     - Message type metadata
     - Cancellation token
   - Sets `context.Message = message`
   - Adds headers from `options.Headers` dictionary
   - Sets `MessageKind = MessageKind.Publish` header
   - Correlates ambient conversation/causation IDs via `PropagateCorrelationIds()`

3. **Dispatch Pipeline Execution** (DispatchEndpoint.ExecuteAsync)
   - **Location**: `/src/Mocha/Endpoints/DispatchEndpoint.cs:35`
   - Waits for pipeline compilation (`_completed.Task`)
   - Executes compiled pipeline with deferred delegate pattern
   - Pipeline order (configured during transport setup):
     1. `DispatchSerializerMiddleware` — serializes message to envelope body using content negotiation (JSON/Protocol Buffers/etc)
     2. Custom user middlewares
     3. `DispatchInstrumentationMiddleware` — Activity/OpenTelemetry tracing
     4. Endpoint-specific middleware
   
4. **Envelope Construction** (DispatchSerializerMiddleware)
   - Creates `MessageEnvelope` with:
     - `MessageId` — auto-generated v7 UUID if not set
     - `CorrelationId` — auto-generated v7 UUID if not set
     - `ConversationId` — propagated from ambient context if available
     - `CausationId` — set to previous message's MessageId if in reply flow
     - `SourceAddress` — endpoint's address URI
     - `DestinationAddress` — null for publish (fanout to all subscribers)
     - `ContentType` — negotiated (application/json, etc)
     - `MessageType` — fully qualified CLR type name
     - `Body` — serialized message bytes
     - `Headers` — metadata headers dictionary
   
5. **KafkaDispatchEndpoint.DispatchAsync** (Transport-Specific)
   - **Location**: `/src/Mocha.Transport.Kafka/KafkaDispatchEndpoint.cs:46-122`
   - Retrieves shared producer: `connectionManager.Producer`
   - Resolves topic name from endpoint configuration: `Topic!.Name`
   - Ensures topic exists (lazy provisioning if configured)
   - **Key Selection**: Uses CorrelationId or MessageId as message key (for partitioning)
   - **Header Mapping**: Converts envelope fields to Kafka headers (UTF-8 encoded strings):
     - `mc_message_id`, `mc_correlation_id`, `mc_conversation_id`, `mc_causation_id`
     - `mc_source`, `mc_destination`, `mc_response`, `mc_fault`
     - `mc_content_type`, `mc_message_type`, `mc_sent_at`, `mc_enclosed_types`
     - Custom headers from envelope.Headers
   - **Body Optimization**: Uses `MemoryMarshal.TryGetArray()` to avoid allocation when buffer is already backed by byte[]
   - **Producer Callback**: Creates `TaskCompletionSource` and tracks in-flight dispatch
   - Calls `producer.Produce(topicName, message, deliveryCallback)` with non-blocking async wrapper
   - Callback fires on librdkafka delivery-report thread:
     - Success: `tcs.TrySetResult()`
     - Error: `tcs.TrySetException(new KafkaException(report.Error))`
     - Untracked from `_inflightDispatches` after callback
   - Awaits `tcs.Task` (caller blocks until ack/error from broker)

6. **Context Cleanup**
   - Dispatch context returned to pool
   - Reusable for next operation

### Error Handling
- Serialization failures: exception propagates to caller
- Producer send failures: wrapped in `KafkaException` and propagated
- Validation errors (missing topic): `InvalidOperationException`

### Key Design Decisions
- **One shared producer**: All dispatch endpoints share a single producer instance for efficiency (thread-safe via librdkafka)
- **Blocking semantics**: Caller waits for broker ack before returning (not async in a fire-and-forget sense)
- **Header strategy**: All correlation/routing metadata in headers, body is opaque serialized message
- **Key-based partitioning**: CorrelationId/MessageId as key ensures related messages go to same partition for ordering

---

## 2. Consume Flow

**Pattern**: Single consumer per receive endpoint continuously polls and processes messages

### Startup Phase (Runtime.StartAsync → Transport.StartAsync)

**Location**: `/src/Mocha/Runtime/MessagingRuntime.cs:116-129`

1. **Transport Initialization** (MessagingRuntime.StartAsync)
   - Iterates transports in registered order
   - Calls `transport.StartAsync(this, cancellationToken)`

2. **Connection Manager Setup** (KafkaMessagingTransport.OnBeforeStartAsync)
   - **Location**: `/src/Mocha.Transport.Kafka/KafkaMessagingTransport.cs:82-94`
   - Ensures producer created: `ConnectionManager.EnsureProducerCreated()`
     - Double-checked locking pattern
     - ProducerConfig: Acks.All, EnableIdempotence=true, LingerMs=5, BatchNumMessages=10000
     - Delivery reports enabled for async ack tracking
   - If auto-provisioning enabled: calls `ConnectionManager.ProvisionTopologyAsync(topics, cancellationToken)`
     - Uses admin client to CreateTopicsAsync
     - Ignores TopicAlreadyExists errors

3. **Receive Endpoint Startup** (ReceiveEndpoint.OnStartAsync)
   - **Location**: `/src/Mocha.Transport.Kafka/KafkaReceiveEndpoint.cs:56-77`
   - Creates consumer group consumer: `connectionManager.CreateConsumer(groupId, logger)`
     - ConsumerConfig: EnableAutoCommit=false, AutoOffsetReset=Earliest, MaxPollIntervalMs=600s
     - Configurable overrides via `consumerConfigOverrides` action
   - Subscribes to topic: `consumer.Subscribe(Topic.Name)`
   - Starts long-running consume loop: `Task.Factory.StartNew(ConsumeLoopAsync, ..., TaskScheduler.Default).Unwrap()`
     - Uses default scheduler for background thread pool
     - CancellationTokenSource for graceful shutdown

### Consume Loop (Continuous Operation)

**Location**: `/src/Mocha.Transport.Kafka/KafkaReceiveEndpoint.cs:79-126`

1. **Poll and Error Handling**
   ```csharp
   while (!cancellationToken.IsCancellationRequested)
   {
       ConsumeResult<byte[], byte[]>? result;
       try
       {
           result = consumer.Consume(cancellationToken);  // Blocking poll
       }
       catch (ConsumeException ex)
       {
           // Log error, continue loop (transient)
           _logger.KafkaConsumeError(ConsumerGroupId, ex.Error.Reason);
           continue;
       }
       
       if (result is null) continue;  // Null on timeout/EOF
   }
   ```
   - Blocking poll with configurable timeout (default 1s in librdkafka)
   - Transient consume errors logged but don't kill loop
   - Null result handled gracefully (no message ready)

2. **Message Dispatch to Middleware Pipeline**
   ```csharp
   await ExecuteAsync(
       static (context, state) =>
       {
           var feature = context.Features.GetOrSet<KafkaReceiveFeature>();
           feature.ConsumeResult = state.result;
           feature.Consumer = state.consumer;
       },
       (result, consumer),
       cancellationToken);
   ```
   - Injects Kafka-specific feature into receive context
   - Invokes receive middleware pipeline (see Middleware Order below)
   - Fatal exceptions caught, logged, loop continues

3. **Receive Middleware Pipeline Order**
   - **Location**: Various files in `/src/Mocha/Middlewares/Receive/`
   
   Order of execution (outer to inner):
   
   a. **KafkaParsingMiddleware**
      - **Location**: `/src/Mocha.Transport.Kafka/Middlewares/Receive/KafkaParsingMiddleware.cs`
      - Extracts `KafkaReceiveFeature` with ConsumeResult
      - Calls `KafkaMessageEnvelopeParser.Instance.Parse(consumeResult)`
      - Creates `MessageEnvelope` from Kafka headers/body
      - Sets envelope on context: `context.SetEnvelope(envelope)`
   
   b. **ReceiveInstrumentationMiddleware**
      - Wraps span for OpenTelemetry/Activity tracing
   
   c. **MessageTypeSelectionMiddleware**
      - Extracts MessageType from envelope headers
      - Sets `context.MessageType` for downstream routing
   
   d. **RoutingMiddleware**
      - **Location**: `/src/Mocha/Middlewares/Receive/RoutingMiddleware.cs:15-51`
      - Uses `IMessageRouter.GetInboundByEndpoint(endpoint)` to get configured routes
      - Matches consumers where:
        - `route.MessageType == messageType` OR
        - `messageType.EnclosedMessageTypes.Contains(route.MessageType)` (polymorphic matching)
      - Adds matched consumers to `ReceiveConsumerFeature.Consumers` list
      - Continues pipeline
   
   e. **ReplyReceiveMiddleware** (if reply topic configured)
      - **Location**: `/src/Mocha/Middlewares/Receive/ReplyReceiveMiddleware.cs`
      - Adds `ReplyConsumer` to feature's consumer list (handles request-reply responses)
   
   f. **ReceiveFaultMiddleware**
      - **Location**: `/src/Mocha/Middlewares/Receive/ReceiveFaultMiddleware.cs:26-52`
      - Wraps downstream execution in try-catch
      - On exception:
        - If message has `ResponseAddress` (request expects reply):
          - Sends `NotAcknowledgedEvent` to response topic
          - Routes fault info in headers
        - Else:
          - Dispatches to error endpoint (configured for receive endpoint)
      - Sets `MessageConsumed = true` to prevent duplicate handling downstream
   
   g. **ConsumerExecutionMiddleware** (Consumer invocation)
      - Iterates `ReceiveConsumerFeature.Consumers` list
      - For each consumer, invokes application handler:
        - `EventHandler<T>.HandleAsync(context)` — single message
        - `BatchHandler<T>.HandleAsync(batch)` — buffered batch
        - `RequestHandler<TRequest>.HandleAsync(request)` — request-reply
      - Marks `MessageConsumed = true` on success
   
   h. **ReceiveDeadLetterMiddleware** (Terminal safety net)
      - **Location**: `/src/Mocha/Middlewares/Receive/ReceiveDeadLetterMiddleware.cs`
      - If `!MessageConsumed`:
        - Dispatches original envelope to error endpoint
        - Preserves all metadata/body for diagnostics
      - Swallows exceptions (dead-lettering is terminal)
   
   i. **KafkaCommitMiddleware**
      - **Location**: `/src/Mocha.Transport.Kafka/Middlewares/Receive/KafkaCommitMiddleware.cs`
      - On successful pipeline completion:
        - Calls `consumer.Commit(consumeResult)` (synchronous, safe because sequential processing)
        - Offset committed atomically
      - On exception:
        - Does NOT commit
        - Message remains available for redelivery (at-least-once delivery)

### Message Envelope Parsing Details

**Location**: `/src/Mocha.Transport.Kafka/KafkaMessageEnvelopeParser.cs:21-44`

```csharp
MessageEnvelope envelope = new()
{
    MessageId = GetHeaderString(headers, "mc_message_id"),
    CorrelationId = GetHeaderString(headers, "mc_correlation_id"),
    ConversationId = GetHeaderString(headers, "mc_conversation_id"),
    CausationId = GetHeaderString(headers, "mc_causation_id"),
    SourceAddress = GetHeaderString(headers, "mc_source"),
    DestinationAddress = GetHeaderString(headers, "mc_destination"),
    ResponseAddress = GetHeaderString(headers, "mc_response"),
    FaultAddress = GetHeaderString(headers, "mc_fault"),
    ContentType = GetHeaderString(headers, "mc_content_type"),
    MessageType = GetHeaderString(headers, "mc_message_type"),
    SentAt = ParseSentAt(headers),  // Deserialize ISO8601
    EnclosedMessageTypes = ParseEnclosedMessageTypes(headers),  // Split comma-separated
    Headers = BuildHeaders(kafkaHeaders),  // Extract non-standard headers
    Body = consumeResult.Message.Value  // Raw message bytes
};
```

- **Header names**: Prefixed with `mc_` (Mocha convention) to distinguish from custom headers
- **Enclosed types**: Comma-separated string, parsed to ImmutableArray for polymorphic dispatch
- **Custom headers**: Non-standard headers collected into envelope.Headers dictionary

### Key Design Decisions
- **At-least-once delivery**: Offset committed after successful pipeline execution
- **Synchronous commit**: Safe because consume loop is sequential (single-threaded processing per consumer)
- **Sequential processing**: No concurrent message processing per consumer group (vs. parallel consumption)
- **Graceful error handling**: Transient consume errors don't kill loop; fatal exceptions logged but don't break flow
- **Redelivery semantics**: Failed messages stay in partition at last committed offset, redelivered on loop restart

---

## 3. Send Flow

**Pattern**: Targeted delivery to a single specific endpoint/consumer

### Entry Point: `IMessageBus.SendAsync(message, options?, cancellationToken)`

**Location**: `/src/Mocha/Middlewares/DefaultMessageBus.cs:83-131`

1. **Endpoint Resolution**
   - If `options.Endpoint` is specified:
     - Explicit routing: `endpoint = runtime.GetDispatchEndpoint(address)` (address-based)
   - Else:
     - Type-based routing: `endpoint = runtime.GetSendEndpoint(messageType)`
     - Router resolves message type → send endpoint mapping

2. **Metadata Propagation** (vs. Publish)
   - Sets headers: `MessageKind = MessageKind.Send`
   - Propagates reply address: `context.ResponseAddress = options.ReplyEndpoint`
     - If not set, uses transport's reply receive endpoint
   - Propagates fault address: `context.FaultAddress = options.FaultEndpoint`
   - Enables request-reply pattern by establishing reply channel

3. **Dispatch Pipeline Execution**
   - Same pipeline as Publish (serialization → instrumentation → Kafka)
   - **Key difference**: `DestinationAddress` set to target endpoint in envelope
   - Partition key strategy same as Publish (CorrelationId/MessageId)

4. **KafkaDispatchEndpoint Behavior**
   - Same as Publish (topic resolution, serialization, producer callback)
   - Envelope includes reply/fault addresses in headers

### Error Handling
- Same as Publish
- Caller receives exception if send fails

### Key Design Decisions
- **Explicit vs. implicit routing**: Options.Endpoint for point-to-point, type-based for convention
- **Reply address negotiation**: Defaults to transport's reply topic if not specified
- **Metadata propagation**: Headers enable reply routing without explicit topic names in addresses

---

## 4. Request-Reply Flow

**Pattern**: Sender awaits typed response from receiver

### Entry Point: `IMessageBus.RequestAsync<TResponse>(request, options?, cancellationToken)`

**Location**: `/src/Mocha/Middlewares/DefaultMessageBus.cs:141-165, 247-296`

1. **Promise Registration** (RequestAndWaitAsync)
   - Generates unique `correlationId = Guid.NewGuid().ToString()`
   - Registers deferred promise: `var waitHandle = _deferredResponseManager.AddPromise(correlationId)`
   - **Location**: `/src/Mocha/DeferredResponseManager.cs:21-38`
   - Creates:
     - `TaskCompletionSource<object?>` for response
     - `CancellationTokenSource` with 2-minute timeout (configurable)
     - Cancellation registered to timeout promise after duration
   - Returns to pool via `_matches.TryAdd(correlationId, promise)`

2. **Request Dispatch**
   - Sets `MessageKind = MessageKind.Request` header
   - Sets `context.CorrelationId = correlationId` (critical for correlation)
   - Sets response address:
     - `context.ResponseAddress = replyEndpoint.Source.Address` (transport's reply receive endpoint)
   - Executes normal dispatch pipeline (serialization → Kafka producer)

3. **Receiver Processing**
   - Receive endpoint consumes request message normally
   - Handler receives request, processes, prepares response
   - Handler calls `await messageBus.ReplyAsync(response, replyOptions)`

4. **Reply Dispatch** (ReplyAsync)
   - **Location**: `/src/Mocha/Middlewares/DefaultMessageBus.cs:199-245`
   - Retrieves transport for reply address: `runtime.GetTransport(options.ReplyAddress)`
   - Gets reply dispatch endpoint: `transport.ReplyDispatchEndpoint`
   - Sets:
     - `context.CorrelationId = options.CorrelationId` (from request)
     - `context.DestinationAddress = options.ReplyAddress` (response topic)
     - `context.MessageKind = MessageKind.Reply`
   - Executes dispatch pipeline → Kafka

5. **Reply Reception and Promise Completion**
   - Reply receive endpoint (configured on transport):
     - **Location**: `/src/Mocha/Middlewares/Receive/ReplyReceiveMiddleware.cs`
     - Adds `ReplyConsumer` to middleware pipeline
     - `ReplyConsumer.HandleAsync()` extracts correlationId from envelope
     - Calls `deferredResponseManager.CompletePromise(correlationId, response)`
   
6. **Caller Unblocks**
   - Promise completes: `await waitHandle.Task` returns response
   - **Location**: `/src/Mocha/Middlewares/DefaultMessageBus.cs:289-295`
   - Validates response type matches expected `TResponse`
   - If wrong type: throws `InvalidOperationException`
   - If timeout: throws `ResponseTimeoutException` (after 2 minutes)

### Timeout Semantics

**Location**: `/src/Mocha/DeferredResponseManager.cs:25-34`

```csharp
var cts = new CancellationTokenSource(timeout.Value, timeProvider);
cts.Token.Register(() =>
{
    if (_matches.TryRemove(correlationId, out var p))
    {
        p.TaskCompletionSource.TrySetException(
            new ResponseTimeoutException(correlationId, p.Timeout));
    }
});
```

- Timeout fires after duration expires
- Promise removed from dictionary
- Exception propagated to caller
- Default timeout: 2 minutes

### Error Handling
- Serialization: exception propagates
- No reply received within timeout: `ResponseTimeoutException`
- Wrong response type: `InvalidOperationException`
- Receiver fault: `NotAcknowledgedEvent` sent back, receiver-side exception propagates to fault handler

### Key Design Decisions
- **Correlation-based matching**: CorrelationId uniquely identifies request-response pairs
- **Promise-based synchronization**: Caller blocked via TaskCompletionSource until response arrives
- **Explicit reply addresses**: Response routing specified in request headers
- **Timeout guarantee**: 2-minute default prevents indefinite blocking on lost messages
- **Reply endpoint per transport**: Transport provides reply-receiving infrastructure

---

## 5. Error Flow

**Pattern**: Failed messages routed to error endpoint with diagnostic context

### Error Sources

1. **Message Handler Exception**
   - Application code in handler throws
   - Caught by consumer execution middleware

2. **Message Deserialization Failure**
   - ContentType not supported
   - Message body malformed for type

3. **Consumer Routing Failure**
   - No consumers registered for message type
   - Type selection middleware can't parse message type

### Error Handling Pipeline

**Location**: `/src/Mocha/Middlewares/Receive/ReceiveFaultMiddleware.cs:26-52`

1. **Detection** (ReceiveFaultMiddleware)
   - Wraps entire downstream pipeline in try-catch
   - Catches any exception from consumers or middleware

2. **Fault Info Creation**
   ```csharp
   var fault = FaultInfo.From(Guid.NewGuid(), provider.GetUtcNow(), ex);
   // Creates FaultInfo with:
   // - ErrorCode (GUID)
   // - Timestamp
   // - Exception details (type, message, stack trace)
   ```

3. **Response Routing Decision**
   - **If message is a request** (`envelope?.ResponseAddress is not null`):
     - Sends `NotAcknowledgedEvent` back to response address
     - Caller's `RequestAsync` receives exception via promise
     - Fault metadata in headers
   
   - **If message is one-way** (publish/send):
     - Skips response, goes to dead letter

4. **Error Endpoint Dispatch** (ReceiveDeadLetterMiddleware)
   - **Location**: `/src/Mocha/Middlewares/Receive/ReceiveDeadLetterMiddleware.cs:22-59`
   - Final safety net if no consumers processed message
   - If `!MessageConsumed`:
     ```csharp
     dispatchContext.Envelope = context.Envelope;  // Original envelope intact
     await errorEndpoint.ExecuteAsync(dispatchContext);
     ```
   - Re-dispatches original envelope to error topic
   - Preserves all metadata for diagnostics
   - Error endpoint is transport-specific (e.g., `topic_name_error` in Kafka)

5. **Offset Behavior**
   - **On successful error routing**: Offset committed (message processed)
   - **On error endpoint dispatch failure**: Exception rethrown, offset not committed
   - Message stays in partition for recovery

### Error Topic Naming Convention

**Location**: Various convention classes (e.g., KafkaDefaultReceiveEndpointConvention)

- Topic: `orders`
- Error topic: `orders_error`
- Reply topic: `orders_reply`

### Key Design Decisions
- **Request-specific negative ack**: Requesters get immediate `NotAcknowledgedEvent` vs. silence
- **Diagnostic preservation**: Original envelope redispatched to error topic with full context
- **Two-phase error routing**: First try direct reply, then dead letter as fallback
- **At-least-once error routing**: Error endpoint dispatch treated like normal message dispatch (can be retried)

---

## 6. Batch Processing Flow

**Pattern**: Accumulate messages, deliver as batch when size or timeout threshold reached

### Configuration

**Location**: `/src/Mocha/Abstractions/IBatchEventHandler.cs`

```csharp
.AddBatchHandler<OrderAnalyticsBatchHandler>(o =>
{
    o.MaxBatchSize = 100;
    o.BatchTimeout = TimeSpan.FromSeconds(2);
})
```

### BatchCollector Operation

**Location**: `/src/Mocha/Consumers/Batching/BatchCollector.cs`

1. **Message Buffering**
   ```csharp
   public async ValueTask<BufferedEntry<TEvent>> Add(IConsumeContext<TEvent> context)
   {
       var entry = new BufferedEntry<TEvent>(context);
       MessageBatch<TEvent>? batch = null;
       
       lock (_sync)
       {
           _buffer.Add(entry);
           
           if (_buffer.Count >= _maxBatchSize)
           {
               _delay.Cancel();
               batch = FlushBufferLocked(BatchCompletionMode.Size);
           }
           else if (_buffer.Count == 1)
           {
               _delay.Start();  // Start timeout on first message
           }
       }
       
       if (batch is not null)
       {
           await _onBatchReady(batch);
       }
       
       return entry;
   }
   ```
   - Thread-safe with lock
   - Tracks buffered entries for delayed completion
   - Two triggers:
     - **Size**: Batch reaches MaxBatchSize
     - **Time**: Timeout expires

2. **Timeout Handling**
   ```csharp
   private async ValueTask OnDelayElapsed()
   {
       MessageBatch<TEvent>? batch;
       
       lock (_sync)
       {
           if (_disposed || _buffer.Count == 0) return;
           batch = FlushBufferLocked(BatchCompletionMode.Time);
       }
       
       await _onBatchReady(batch);
   }
   ```
   - Fired after BatchTimeout duration
   - Flushes pending messages even if under MaxBatchSize

3. **Batch Handler Invocation**
   ```csharp
   await batchHandler.HandleAsync(batch);
   ```
   - Handler receives `IMessageBatch<T>` interface
   - Batch contains:
     - `Messages` — list of buffered messages
     - `CompletionMode` — how batch was triggered (Size vs. Time)
   - Handler processes all messages in batch
   - On exception: batch processing fails (individual retries handled by consumer)

4. **Back-Pressure Semantics**
   - Batch handler awaited before consuming next message
   - If handler throws, batch is marked failed
   - `BatchRetryExceededException` thrown if retry count exceeded

### Key Design Decisions
- **Synchronous lock on buffer**: No concurrent access to buffer during size checks
- **Lazy timeout start**: Timeout only active when messages present
- **Handler-level batching**: Application handler determines batch semantics (aggregation, etc)
- **Completion mode tracking**: Handler knows whether batch triggered by size or timeout

---

## 7. Startup Flow

**Pattern**: Initialize, configure, and start message bus with all transports and endpoints

### Configuration Phase (Builder)

**Location**: `/src/Mocha/Builder/MessageBusBuilder.cs` and related

```csharp
services.AddMessageBus()
    .AddKafka(t =>
    {
        t.BootstrapServers("localhost:9092");
        t.AutoProvision(true);
        t.DeclareTopic("orders");
        t.Endpoint("orders-endpoint")
            .Topic("orders")
            .Handler<OrderHandler>();
    });
```

1. **Transport Registration**
   - Transport descriptor created (e.g., `KafkaMessagingTransportDescriptor`)
   - Configuration action applied: `t.BootstrapServers()`, `t.DeclareTopic()`, etc
   - Transport configuration compiled to `MessagingTransportConfiguration`

2. **Endpoint Declaration**
   - For each handler, endpoint created or resolved
   - Topic mapping: which topic receives messages for this endpoint
   - Handler registration: consumer added for this endpoint

3. **Convention Application**
   - **Location**: `Conventions/` directories in transport packages
   - Applied to all endpoints during configuration
   - Example conventions:
     - `KafkaDispatchEndpointTopologyConvention` — derive dispatch topic names
     - `KafkaReceiveEndpointTopologyConvention` — derive receive topic names and error topics

### Initialization Phase (Host.StartAsync)

**Location**: Various Startup methods

1. **Transport.Initialize** (IMessagingSetupContext)
   - Creates configuration via `CreateConfiguration(setupContext)`
   - Calls `OnAfterInitialized(setupContext)` for transport-specific setup
   
   **For Kafka** (KafkaMessagingTransport.OnAfterInitialized):
   - **Location**: `/src/Mocha.Transport.Kafka/KafkaMessagingTransport.cs:40-75`
   - Resolves bootstrap servers from configuration
   - Builds topology URI from first bootstrap server:
     ```csharp
     var firstServer = bootstrapServers.Split(',')[0].Trim();
     var parts = firstServer.Split(':');
     var builder = new UriBuilder
     {
         Scheme = "kafka",
         Host = parts[0],
         Port = parts.Length > 1 && int.TryParse(parts[1], out var port) ? port : 9092,
         Path = "/"
     };
     ```
   - Creates topology instance: `KafkaMessagingTopology`
     - Registers all topics from configuration
   - Creates connection manager: `new KafkaConnectionManager(...)`

2. **Receive Endpoint Discovery**
   - Runtime iterates all configured receive endpoints
   - Calls `endpoint.Initialize(context, configuration)`
   - Each endpoint calls `DiscoverTopology(context)` — convention-based discovery
   - Sets up middleware pipelines (parsing, routing, fault, dead letter, commit)

3. **Dispatch Endpoint Discovery**
   - Runtime iterates all configured dispatch endpoints
   - Calls `endpoint.Initialize(context, configuration)`
   - DiscoverTopology applied
   - Middleware compiled (serialization, instrumentation)

4. **Endpoint Completion** (DispatchEndpoint.Complete)
   - **Location**: `/src/Mocha/Endpoints/DispatchEndpoint.cs:64-72`
   - Signals completion: `_completed.SetResult(true)`
   - Compiles middleware pipeline
   - Volatilely writes real pipeline, replacing deferred delegate
   - Any pending dispatch operations unblock and execute compiled pipeline

### Startup Phase (Runtime.StartAsync)

**Location**: `/src/Mocha/Runtime/MessagingRuntime.cs:116-129`

1. **Transport Connection Establishment**
   - For each transport:
     ```csharp
     await transport.StartAsync(this, cancellationToken);
     ```
   
   **For Kafka** (KafkaMessagingTransport):
   - Calls `OnBeforeStartAsync()`:
     - Creates shared producer: `ConnectionManager.EnsureProducerCreated()`
       - ProducerConfig: Acks.All, EnableIdempotence, LingerMs=5, BatchNumMessages=10k
     - Provisions topology (creates topics):
       ```csharp
       await ConnectionManager.ProvisionTopologyAsync(
           _topology.Topics.Where(t => t.AutoProvision ?? true),
           cancellationToken);
       ```
   
   - Calls `OnAfterStartAsync()` (base):
     - For each receive endpoint:
       ```csharp
       await endpoint.StartAsync(context, cancellationToken);
       ```
     - Each endpoint starts consume loop (long-running task)
     - Consumer created and subscribed to topic

2. **Runtime Marked Started**
   ```csharp
   IsStarted = true;
   ```
   - All transports and endpoints are now operational
   - MessageBus operations now allowed

### Key Design Decisions
- **Deferred pipeline compilation**: Endpoints accept dispatches before pipeline compiled via TaskCompletionSource
- **Two-phase topic provisioning**: Configuration declares topics, startup creates them
- **Sequential startup**: Transports started in registered order (can be important if shared infrastructure)
- **Long-running consume loops**: Receive endpoints run background task per consumer group
- **Auto-provisioning opt-in**: Topics created only if AutoProvision=true

---

## 8. Shutdown Flow

**Pattern**: Graceful termination with message draining and resource cleanup

### Graceful Shutdown Initiation

**Location**: Host.StopAsync or app disposal

1. **Transport.StopAsync** (IMessagingRuntimeContext)
   - For each receive endpoint:
     ```csharp
     await endpoint.StopAsync(context, cancellationToken);
     ```

2. **Receive Endpoint Shutdown** (KafkaReceiveEndpoint.OnStopAsync)
   - **Location**: `/src/Mocha.Transport.Kafka/KafkaReceiveEndpoint.cs:128-158`
   ```csharp
   if (_cts is not null)
   {
       await _cts.CancelAsync();  // Signal consume loop to stop
   }
   
   if (_consumeLoopTask is not null)
   {
       try
       {
           await _consumeLoopTask;  // Wait for loop to finish
       }
       catch (OperationCanceledException)
       {
           // Expected
       }
   }
   
   if (_consumer is not null)
   {
       _consumer.Close();  // Close consumer
       _consumer.Dispose();
   }
   ```
   
   Order:
   1. Signal cancellation via CancellationTokenSource
   2. Consume loop receives cancellation and exits
   3. Await loop task completion (with timeout from host)
   4. Close and dispose consumer
   
   **Important**: No explicit offset commit on shutdown — last committed offset is respected on restart

3. **Producer Cleanup** (KafkaConnectionManager.DisposeAsync)
   - **Location**: `/src/Mocha.Transport.Kafka/Connection/KafkaConnectionManager.cs:202-229`
   ```csharp
   if (_producer is not null)
   {
       // Flush pending messages before disposing
       _producer.Flush(TimeSpan.FromSeconds(10));
       _producer.Dispose();
   }
   
   // Cancel any remaining in-flight dispatch TCS instances
   foreach (var tcs in _inflightDispatches.Keys)
   {
       tcs.TrySetCanceled();
   }
   ```
   
   1. Flush producer (waits up to 10s for pending acks)
   2. Dispose producer
   3. Cancel any inflight dispatches still pending (after flush timeout)

### In-Flight Message Handling

**Key Decision**: 
- **In-flight dispatches**: Awaited during Flush (up to 10 seconds)
- **In-flight consumes**: Receive loop stops polling but processing completes before consumer close
- **No message loss**: Graceful shutdown prioritizes message completion over speed

### Resource Disposal

```csharp
await runtime.DisposeAsync();
```

- Iterates consumers (handlers, sagas, etc)
- Calls `DisposeAsync()` on each
- Releases any held resources (database connections, etc)

### Key Design Decisions
- **Flush with timeout**: Producer flushed before closing (prevents message loss)
- **Sequential receive endpoint shutdown**: Each endpoint stops consuming before next
- **Inflight cancellation**: Messages not acked after flush timeout are cancelled (unblocks callers)
- **Consumer close on shutdown**: Committed offsets preserved for restart
- **No forced kill**: Graceful shutdown respects in-flight operations up to timeout

---

## Cross-Cutting Concerns

### Message Correlation

**Identifiers**: 
- `MessageId` — unique per message (auto-generated v7 UUID)
- `CorrelationId` — groups related messages (same for request/reply pair)
- `ConversationId` — groups logical conversation (propagated through chain)
- `CausationId` — points to message that caused this one

**Propagation**:
- Ambient via `ConsumeContextAccessor` during receive
- Manually via `SendOptions`/`ReplyOptions`
- Automatic in request-reply via correlation ID matching

### Content Type Negotiation

**Location**: Dispatch serialization middleware

- Default: application/json (if not configured)
- Per-message: set in SendOptions
- Serializer registered by content type
- Deserialization on receive uses same ContentType header

### Concurrency Model

**Thread Safety**:
- **Dispatch endpoint**: Thread-safe producer (librdkafka)
- **Receive endpoint**: Single-threaded consume loop per endpoint
- **Router**: Thread-safe with immutable hash sets
- **Connection manager**: Double-checked locking for producer creation
- **Batch collector**: Lock-based for buffer access
- **Promise manager**: ConcurrentDictionary for thread-safe lookups

**Note**: No parallel message processing per consumer — sequential guarantee enables simple at-least-once semantics.

### Resource Pooling

**Context Pooling**:
- `DispatchContext` pooled in `DefaultMessageBus`
- **Location**: `/src/Mocha/Utils/Pooling/DispatchContextPool.cs`
- Reuse across operations to reduce allocation
- Reset on return to pool (features cleared, headers reset, etc)

**Producer/Consumer**:
- Single shared producer per transport (efficiency)
- One consumer per receive endpoint (isolation)
- No pooling (lifecycle matches endpoint lifetime)

---

## Complete Message Lifecycle Example

```
User calls: messageBus.PublishAsync(new OrderPlacedEvent { ... })
     ↓
DefaultMessageBus.PublishAsync<T>
  - Resolve message type: MessageType
  - Resolve endpoint: PublishEndpoint (routes to Kafka)
  - Pool DispatchContext
  - Set MessageKind = Publish
  - Propagate correlation IDs
     ↓
Dispatch Pipeline Execution
  - DispatchSerializerMiddleware: Serialize to JSON envelope body
  - DispatchInstrumentationMiddleware: Create Activity span
     ↓
KafkaDispatchEndpoint.DispatchAsync
  - Ensure topic provisioned
  - Select key: CorrelationId or MessageId
  - Build Kafka headers from envelope fields
  - Create Message<byte[], byte[]> with body + headers
  - Create TaskCompletionSource for ack tracking
  - producer.Produce(topicName, message, deliveryCallback)
  - deliveryCallback: Sets result/exception on TCS
  - Await TCS.Task (blocking until broker ack)
     ↓
Kafka Broker
  - Receives message on topic partition
  - Replicates to in-sync replicas
  - Sends ack back to producer
     ↓
KafkaReceiveEndpoint Consume Loop (separate, continuous)
  - consumer.Consume(cancellationToken)  [blocking poll]
  - ConsumeResult<byte[], byte[]> received
     ↓
Receive Middleware Pipeline
  - KafkaParsingMiddleware: Parse Kafka headers → MessageEnvelope
  - MessageTypeSelectionMiddleware: Extract message type from headers
  - RoutingMiddleware: Find consumers for this type
  - ReceiveFaultMiddleware: Wrap in try-catch
  - ConsumerExecutionMiddleware: Invoke handlers
    * OrderPlacedEventHandler.HandleAsync(context)
  - ReceiveDeadLetterMiddleware: If not consumed, forward to error topic
  - KafkaCommitMiddleware: Commit offset
     ↓
Commit
  - consumer.Commit(consumeResult)
  - Offset persisted in Kafka __consumer_offsets topic
     ↓
Consumer (Handler) Receives Message
  - Publishes OrderShippedEvent (or any other action)
  - Cycle repeats for new message
```

---

## Testing Hooks

Key classes for testing:

- **Mock Kafka**: InMemory transport or test containers
- **Message inspection**: Receive context features for assertions
- **Fault simulation**: Throw in handlers to test error routing
- **Batch testing**: Configure small MaxBatchSize and short timeout
- **Correlation testing**: Verify IDs propagated through correlation chains
- **Timeout testing**: Use TimeProvider for deterministic timeout simulation

