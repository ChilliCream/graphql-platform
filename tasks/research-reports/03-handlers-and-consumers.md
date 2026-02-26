I now have comprehensive research. Let me compile everything into the structured report.

---

# Handlers and Consumers Documentation Research Report

## Current Page Summary

**File:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/handlers-and-consumers.md`

**What the page covers:**

The page is well-structured and covers the full handler surface area for Mocha. It opens with a clear mental model: handlers are what developers write; consumers are what Mocha wraps them in. The document then proceeds through:

1. A working tutorial for registering an event handler (`IEventHandler<T>`) with verification output
2. How-to sections for request handlers (`IEventRequestHandler<TReq, TRes>`), send/command handlers (`IEventRequestHandler<TReq>`), batch handlers (`IBatchEventHandler<T>`), and the low-level `IConsumer<T>` interface
3. A reference section with tables for all interfaces, `BatchOptions`, `IMessageBatch<T>`, and the `IEventRequest<TResponse>` constraint
4. An architecture section explaining how each handler maps to a consumer adapter, the consumer pipeline, and the `IHandler` base interface
5. A troubleshooting section covering the three most common failure modes

**Page strengths:**

- The tutorial section is concrete and closes the loop with expected console output - developers know immediately whether their setup worked
- The interface summary table is one of the clearest in any messaging framework doc
- The handler-to-consumer mapping table exposes internal infrastructure in a useful way
- The `IHandler` static abstract members explanation is technically precise and differentiates Mocha's compile-time approach from reflection-based frameworks
- The troubleshooting section covers real failure modes with actionable steps

**Gaps identified (discussed in Recommendations):**

- No explanation of why the three handler types exist conceptually (event vs. command vs. request/reply) - readers learn the interface names before understanding the messaging pattern they encode
- No discussion of what happens when a handler throws - exception propagation path is not described
- `IConsumeContext<T>` property list is given as a prose sentence rather than a reference table, making it harder to scan
- No mention of idempotency considerations
- No explanation of how the scoped DI lifetime is created per message (the "unit of work" concept)
- No guidance on testing handlers (even a pointer to a testing page)
- The `IEventRequest<TResponse>` interface naming is confusing for a "command" (not an event)

---

## Competitor Analysis

### NServiceBus

**Primary doc:** [Handlers - NServiceBus - Particular Docs](https://docs.particular.net/nservicebus/handlers/)

**How they introduce the concept:**

NServiceBus opens bluntly: "NServiceBus will take a message from the queue and hand it over to one or more message handlers." There is no framing of why handlers exist or what problem they solve. The interface is shown immediately: implement `IHandleMessages<T>`, write an async `Handle(T message, IMessageHandlerContext context)` method.

**Single interface for all message types.** NServiceBus uses one interface (`IHandleMessages<T>`) for commands, events, and replies. The distinction between message types is enforced through marker interfaces on the message classes themselves (`ICommand`, `IEvent`, `IMessage`), not through separate handler interfaces. A handler implementing `IHandleMessages<SubmitOrder>` processes whatever message type `SubmitOrder` is declared as. The routing layer - not the handler interface - controls whether a message goes to one consumer (send) or many (publish).

This is a design choice worth noting: NServiceBus deliberately chose a uniform handler interface and pushes the command/event distinction onto the message type itself. Mocha does the inverse: separate handler interfaces (`IEventHandler<T>` vs `IEventRequestHandler<T>`) encode the routing intent, and the message class needs no marker interface for pub/sub events.

**DI/scoping:** NServiceBus creates a child container ("child lifetime scope") per transport message. Dependencies registered as `InstancePerUnitOfWork` (which maps to `AddScoped<T>()` in MSDI) are resolved once and shared across all handlers processing the same message. This is particularly relevant when multiple handlers for the same message need to share a database session. The documentation covers this on a separate page: [Child containers - NServiceBus](https://docs.particular.net/nservicebus/dependency-injection/child-lifetime). The main handlers page does not explain scoping at all - you have to find the DI page.

**`IMessageHandlerContext`:** Every handler receives a context object alongside the message. This context provides `Send()`, `Publish()`, `Reply()`, `CancellationToken`, and access to message headers including `CorrelationId`. This is key: in NServiceBus, you cannot publish or send from a handler without the context - there is no ambient bus you can inject. The context is the messaging gateway. Using `IMessageSession` or `IEndpointInstance` inside a handler instead of the provided context is explicitly called out as wrong.

**Multiple handlers for the same message:** NServiceBus explicitly covers the transactional implications. If multiple handlers process one message, they execute as a single unit of work. If any handler fails, all retry. The documentation covers what this means for idempotency and offers three solutions: make handlers idempotent, use `SendLocal` to decompose into separate messages, or host handlers in separate endpoints. This is one of the most useful pieces of documentation any messaging framework provides on this topic.

**Handler ordering:** Separate page ([Handler Ordering](https://docs.particular.net/nservicebus/handlers/handler-ordering)) covers how to control execution sequence when multiple handlers process the same message. The API uses `AddHandler<>()` calls to establish ordering, with unspecified handlers executing non-deterministically. They note that handler ordering was historically used for cross-cutting concerns (auth, logging) but the pipeline is now the recommended approach.

**Batch processing:** NServiceBus has no native batch handler interface. High-throughput bulk processing requires custom pipeline behaviors or external batching patterns.

**What NServiceBus does well:**

- Multiple-handler transactional semantics are documented clearly and honestly, including the hard problems
- The `IMessageHandlerContext` as the sole gateway to messaging operations within a handler is a clean design that prevents ambient-service anti-patterns - and the documentation is explicit about it
- Async task requirements are documented precisely (no null Tasks)
- Testing support (`NServiceBus.Testing`) is prominently linked

**What NServiceBus does poorly:**

- A single `IHandleMessages<T>` for all message types means no interface-level distinction between event handlers and command handlers - the documentation doesn't help developers understand when they're implementing pub/sub vs point-to-point
- The main handlers page is thin; DI scoping, context details, and handler lifecycle are scattered across multiple pages without clear links from the main page
- No batch handler support
- The page does not explain what happens to the message when a handler throws

---

### MassTransit

**Primary doc:** [Consumers - MassTransit](https://masstransit.io/documentation/concepts/consumers)

**How they introduce the concept:**

MassTransit uses the term "consumer" uniformly for what other frameworks call "handlers." The intro states: "A message consumer, the most common consumer type, is a class that consumes one or more message types." They show the interface immediately:

```csharp
public interface IConsumer<in TMessage> : IConsumer
    where TMessage : class
{
    Task Consume(ConsumeContext<TMessage> context);
}
```

Notice: unlike Mocha's `HandleAsync(T message, CancellationToken)` or NServiceBus's `Handle(T message, IMessageHandlerContext)`, MassTransit passes only the `ConsumeContext<T>`. The message itself is accessed via `context.Message`. There is no separate context vs. message - everything is unified in the context object.

**One interface, all scenarios.** Like NServiceBus, MassTransit uses `IConsumer<T>` for all message types. The routing behavior (pub/sub vs. point-to-point) is determined by the transport configuration and endpoint setup, not the consumer interface. This is explicitly stated as "The Hollywood Principle" - the framework calls your code; you don't call the framework.

**DI/scoping:** MassTransit creates a container scope for each received message, resolves a consumer instance, calls `Consume`, and disposes the scope. Consumers are registered as scoped. The documentation states: "MassTransit creates a container scope, resolves a consumer instance, and executes the Consume method." Dependencies should be registered as scoped when possible unless they are singletons. This is covered on the consumers page directly, not on a separate DI page - a usability improvement over NServiceBus.

**`ConsumeContext<T>` properties:** The context provides `Message`, `MessageId`, `CorrelationId`, `ConversationId`, `RequestId`, `Headers`, `CancellationToken`, `Publish()`, `Send()`, `RespondAsync()`, `Fault<T>()`, and access to the raw message body. Properties published or sent from within a consumer automatically inherit the `ConversationId` from the consumed message and set `InitiatorId` from its `CorrelationId`. MassTransit's documentation is sparse on the property-by-property breakdown; the full list requires reading the source interface.

**Batch consumers:** `IConsumer<Batch<T>>` is the batch interface. The documentation notes the efficiency argument: "receiving one hundred messages and then writing the content of those messages using a single storage operation may be significantly more efficient." Configuration requires tuning `PrefetchCount` and `ConcurrentMessageLimit` to match the batch size, otherwise the batch never fills to the configured limit. The critical caveat: "If PrefetchCount is lower than the batch limit, performance will be limited by the time limit as the batch size will never be reached." The docs acknowledge that for batch timeout tuning "it is best to experiment" - which is honest but not particularly helpful. Compare to Mocha's explicit `BatchOptions` reference table with defaults and validation rules.

**Consumer definitions:** MassTransit adds a `ConsumerDefinition<T>` class as an optional companion to each consumer, allowing endpoint naming, concurrency limits, and retry/outbox middleware to be co-located with the consumer. This is a strong pattern: the definition acts as a configuration object that keeps consumer-specific infrastructure decisions close to the consumer code without polluting the consumer class itself. Mocha uses the fluent `AddBatchHandler<T>(opts => ...)` lambda for similar configuration inline at registration.

**Job consumers:** A separate consumer type ([Job Consumers - MassTransit](https://masstransit.io/documentation/patterns/job-consumers)) addresses long-running tasks that exceed broker message lock timeouts. These use a saga state machine internally to persist job state, allowing processing to outlast any single broker lock window. This is a capability Mocha does not document equivalently.

**Skipped messages:** Messages that arrive with no registered consumer go to a `_skipped` queue with headers identifying the source. This is a useful operational detail that Mocha does not document.

**What MassTransit does well:**

- The `ConsumeContext<T>` as the unified access point is consistent and predictable
- Consumer definitions as a pattern keep infrastructure configuration co-located with consumer code
- DI scoping is explained on the consumers page itself
- Batch consumer `PrefetchCount` interaction is documented (even if sparsely)
- The "skipped messages" behavior is documented - an operational detail that matters in production

**What MassTransit does poorly:**

- No interface-level distinction between event consumers and command consumers - everything is `IConsumer<T>`, and the routing intent lives in configuration
- The `ConsumeContext` property surface is not tabulated - you have to read source code or middleware docs to find the full list
- Batch configuration guidance is vague ("experiment with values")
- No worked example that shows the full lifecycle from publish through consume to confirmation
- The "Hollywood Principle" framing is philosophically interesting but does not help a developer debug why their consumer isn't being called

---

### Wolverine

**Primary docs:** [Message Handlers - Wolverine](https://wolverinefx.net/guide/handlers/) | [Message Handler Discovery - Wolverine](https://wolverinefx.net/guide/handlers/discovery)

**How they introduce the concept:**

Wolverine leads with the philosophical difference from other frameworks: convention over configuration. No interface is required. The simplest handler is:

```csharp
public class OrderHandler
{
    public void Handle(OrderPlaced order) { }
}
```

Wolverine discovers this by scanning for types ending in `Handler` or `Consumer` with methods named `Handle()`, `Handles()`, `Consume()`, or `Consumes()`. The first parameter is always the message type.

**Convention-based vs. explicit.** Wolverine supports both conventions and explicit markers (`IWolverineHandler` interface, `[WolverineHandler]` attribute), but strongly favors conventions. This is explicitly a design philosophy: "there is zero runtime Reflection happening within the Wolverine execution runtime pipeline." Instead, Wolverine generates C# source code at startup that wraps each handler with typed, compiled glue code. This is the source of its performance claim and its "magic" reputation.

**Method injection (the most distinctive feature):** Beyond the message parameter, Wolverine handler methods accept additional parameters that are resolved from the DI container, not through the constructor. This mirrors ASP.NET Core minimal APIs. A handler method signature can be:

```csharp
public async Task Handle(
    OrderPlaced order,
    AppDbContext db,
    ILogger<OrderHandler> logger,
    CancellationToken cancellationToken)
```

Dependencies are method-injected per invocation. Constructor injection is also supported for true singletons. This approach means handler classes can have zero fields - they are effectively functions decorated with a class wrapper for discovery purposes.

**Static handlers:** Wolverine also supports static handler methods as a zero-allocation optimization, removing the need for object instantiation per message.

**Cascading messages (the most distinctive architecture concept):** Wolverine handlers can return objects instead of publishing them explicitly. A returned message type is automatically published as a cascading message:

```csharp
public OrderConfirmed Handle(PlaceOrder command)
{
    // process...
    return new OrderConfirmed(command.OrderId);
}
```

This enables "pure function" handlers that are trivially testable without mocks. Tuple returns are supported for cascading multiple messages. This is a fundamentally different approach than Mocha's (`IMessageBus.PublishAsync` in a handler) or NServiceBus/MassTransit's (context.Publish within the handler).

**Compound handlers (middleware via method naming):** Wolverine supports conventional "before/after" methods named `Load`, `Validate`, `Before`, `After`, `Finally` that execute around the main `Handle` method. These serve as inline middleware without requiring the developer to write an explicit pipeline component.

**DI/scoping:** Each message execution creates a DI scope. Constructor-injected dependencies are resolved per handler instantiation (scoped). Method-injected service parameters are resolved from the same scope. The documentation covers this through examples rather than an explicit lifecycle description.

**Multiple handlers for the same message:** Wolverine allows multiple handlers for the same message type. The default behavior executes them as logically combined. The `MultipleHandlerBehavior.Separated` option treats each handler as an independent subscription, giving each its own retry scope. The documentation acknowledges this is a recent addition and that compound handler filtering "lacks sophistication for multi-message scenarios."

**Diagnostic tooling:** Wolverine provides a `DescribeHandlerMatch()` method for diagnosing why a handler is or is not being discovered - a direct answer to the "why isn't my handler being called" problem.

**What Wolverine does well:**

- Cascading message returns enable pure-function handlers that are trivially unit-testable
- Method injection eliminates constructor boilerplate for handler-specific dependencies
- The diagnostic `DescribeHandlerMatch()` method directly addresses the most common developer question
- Compound handler pattern (`Load`/`Validate`/`Handle`/`After`) separates concerns without explicit middleware plumbing
- Zero-reflection runtime pipeline is a genuine performance differentiator

**What Wolverine does poorly:**

- Convention-based discovery is "magic" - developers unfamiliar with the conventions will be confused when a handler is not discovered
- No explicit interface enforcement means typos in method names silently prevent handler registration
- The compound handler method naming convention requires memorizing a set of names (`Load`, `LoadAsync`, `Before`, `BeforeAsync`, `After`, `AfterAsync`, `Finally`, `FinallyAsync`)
- Cascading messages and the `IMessageBus.PublishAsync` path coexist, creating two different idioms for the same operation
- Documentation is dense and assumes familiarity with Wolverine's code generation approach

---

## Best Practices Found

**1. Explain the conceptual model before the API (from competitor analysis)**

NServiceBus, MassTransit, and Wolverine all introduce their handler/consumer types with a one-sentence conceptual framing before showing code. The most effective framing comes from understanding what problem each handler type solves. Mocha's page assumes the reader already knows they need an event handler vs. a request handler. A brief "when to use which" paragraph before the first code block would address this.

**2. Keep DI scoping explanation on the handlers page (MassTransit pattern)**

MassTransit explains that a container scope is created per message on the consumers page itself, not on a separate DI page. NServiceBus buries this in a child containers page. Since DI scoping affects how developers design their handlers (can I inject `DbContext` directly?), it belongs on the handlers page.

**3. Document what happens when a handler throws (gap in all competitors)**

None of the three competitors document the exception propagation path clearly on their handler pages. This is a significant gap. For Mocha, the question is: if `HandleAsync` throws, does the message get retried? Who decides? Is the exception surfaced to the caller in request/reply mode? This is mentioned on the middleware page but not on the handlers page.

**4. Tabulate the consume context interface (gap in all competitors)**

Mocha lists `IConsumeContext<T>` properties as a long prose sentence. MassTransit doesn't tabulate them at all. NServiceBus's `IMessageHandlerContext` capabilities are spread across multiple pages. A clear table with property name, type, and description is the most scannable format for a reference section - which Mocha already uses for `BatchOptions` and `IMessageBatch<T>`.

**5. Show the "sending from within a handler" pattern (from NServiceBus)**

NServiceBus makes it explicit that you send/publish from within a handler using the context object (`context.Send()`, `context.Publish()`), not an injected `IMessageSession`. Mocha's equivalent would be showing how to use `IMessageBus` injected via the constructor to publish additional messages from within a handler, and whether those publications participate in the same transaction/correlation as the inbound message.

**6. Address idempotency (from NServiceBus - their strongest unique contribution)**

NServiceBus's documentation of what happens with multiple handlers and the transactional unit of work is the most thoughtful handler documentation of the three competitors. Even if Mocha doesn't support multiple handlers for the same message type, the question of handler idempotency (what happens if the message is delivered twice?) is universal and should be addressed.

**7. The diagnostic "why isn't my handler being called" section (from Wolverine)**

Mocha's troubleshooting section covers "Handler not invoked" but only checks registration and type matching. Wolverine's `DescribeHandlerMatch()` diagnostic is the gold standard for this problem. Mocha's equivalent would be verifying the bus is started (already covered), verifying transport connection, and checking whether the message was routed to the correct endpoint.

---

## External References

**Enterprise Integration Patterns (authoritative primary source):**

- [Message Endpoint pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageEndpoint.html) - Defines the abstraction that handlers implement. The pattern description: "Connect an application to a messaging channel using a Message Endpoint, a client of the messaging system."
- [Event-Driven Consumer pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/EventDrivenConsumer.html) - Defines the push-based consumer model that Mocha's handlers implement. "An object that is invoked by the messaging system when a message arrives on the consumer's channel."
- [Competing Consumers pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CompetingConsumers.html) - Explains multiple consumer instances processing from a shared queue, which is the concurrency model for Mocha's consumers under load.
- [Service Activator pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessagingAdapter.html) - Defines the bridge between messaging and synchronous service invocation, which is precisely what Mocha's consumer adapters do.
- [Messaging Endpoint patterns overview](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessagingEndpointsIntro.html) - Groups all endpoint-related patterns including Polling Consumer vs. Event-Driven Consumer.

**CQRS and command handler patterns:**

- [CQRS Pattern - Azure Architecture Center](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs) - Authoritative reference for why command handlers and event handlers have different semantics. The Microsoft pattern shows `ICommandHandler<T>` explicitly and explains commands should "represent specific business tasks instead of low-level data updates."

**Competitor handler documentation (for cross-referencing):**

- [NServiceBus Handlers](https://docs.particular.net/nservicebus/handlers/) - Covers `IHandleMessages<T>`, async task requirements, multiple handlers per message
- [NServiceBus Child Containers (DI Scoping)](https://docs.particular.net/nservicebus/dependency-injection/child-lifetime) - `InstancePerUnitOfWork` and scoped handler lifetime per message
- [NServiceBus Handler Ordering](https://docs.particular.net/nservicebus/handlers/handler-ordering) - Controls execution sequence for multiple handlers
- [NServiceBus Async Handlers](https://docs.particular.net/nservicebus/handlers/async-handlers) - Task requirements, CancellationToken, IMessageHandlerContext context
- [MassTransit Consumers](https://masstransit.io/documentation/concepts/consumers) - `IConsumer<T>`, `ConsumeContext<T>`, batch consumers `IConsumer<Batch<T>>`, consumer definitions, skipped messages
- [MassTransit Job Consumers](https://masstransit.io/documentation/patterns/job-consumers) - Long-running task consumers with saga state persistence
- [Wolverine Message Handlers](https://wolverinefx.net/guide/handlers/) - Convention-based discovery, method injection, cascading messages, compound handlers
- [Wolverine Handler Discovery](https://wolverinefx.net/guide/handlers/discovery) - Naming conventions, explicit markers, diagnostic tooling
- [Wolverine Cascading Messages](https://wolverinefx.net/guide/handlers/cascading.html) - Return values as outgoing messages, tuple support, testability implications

---

## Recommendations

The following are specific suggestions for the handlers-and-consumers.md page, in priority order.

**Priority 1: Add a "When to use which handler" orientation paragraph**

Before the first code block, add a short paragraph that maps handler type to messaging intent. Currently the page jumps to `IEventHandler<T>` without explaining why three handler interfaces exist. The pattern is:

- `IEventHandler<T>` - Someone published a notification. Multiple handlers can receive it. No reply expected.
- `IEventRequestHandler<TReq, TRes>` - A caller sent a request and is waiting for a typed response.
- `IEventRequestHandler<TReq>` - A caller sent a command for processing with no typed reply, only optional acknowledgment.
- `IBatchEventHandler<T>` - Same as `IEventHandler<T>` but accumulates messages before processing.
- `IConsumer<T>` - Explicit access to envelope metadata is needed.

This maps directly to the EIP patterns: Event-Driven Consumer, Request-Reply (from the EIP catalog), and Service Activator.

**Priority 2: Add an `IConsumeContext<T>` properties reference table**

The current page lists all context properties as a single prose sentence (line 398). This is the most reference-looked-up section - developers reach for it when they need a specific property. Convert this to a table with columns: Property, Type, Description. Use the same format as `BatchOptions` and `IMessageBatch<T>` tables already on the page.

**Priority 3: Add a DI scoping explanation to the interface reference section**

Currently the page mentions "scoped service provider" in passing (lines 57, 398, 423). Add a dedicated paragraph or callout box explaining that Mocha creates a new DI scope per message, that constructor dependencies are resolved from that scope, and that this means `DbContext` and other scoped services are safe to inject directly. Reference this from all handler code examples. This is the single most common question new users have about handler patterns in .NET messaging frameworks, and all three competitors document it - though MassTransit does it best by keeping it on the consumers page.

**Priority 4: Add exception/error behavior to each handler section**

Add a sentence or two to each handler implementation section (or a unified explanation at the bottom of the How-To section) covering: what happens when `HandleAsync` throws, whether the message is retried, how the exception surfaces to a caller in request/reply mode, and whether faults are emitted. This is one of the most meaningful gaps relative to competitors. The middleware page likely covers retry, but the handlers page should at minimum link to that context.

**Priority 5: Add a "publishing from within a handler" example**

Show how to inject `IMessageBus` into an event handler's constructor and use it to publish a second message as a result of handling the first. This is the most common real-world handler pattern after simple logging handlers. Also clarify whether messages published this way inherit the `CorrelationId` and `ConversationId` from the inbound message (MassTransit does this automatically; NServiceBus requires the context; Wolverine does it via cascade returns). This has significant implications for distributed tracing.

**Priority 6: Consider renaming `IEventRequest<TResponse>` in a future version**

This is a naming observation rather than a doc fix. The marker interface `IEventRequest<TResponse>` on command/request classes (line 108) uses "Event" in what is semantically a "Command" or "Request." For a reader who has absorbed the pub/sub vs. request/reply distinction, the name is confusing. MassTransit avoids this by not requiring marker interfaces at all. NServiceBus uses `ICommand` and `IMessage` clearly. This does not require a doc change now but is worth capturing as a naming inconsistency that will generate reader questions.

**Priority 7: Add a pointer to testing handlers**

Wolverine prominently links to testing patterns for handlers. NServiceBus links to `NServiceBus.Testing`. The Mocha page ends with a "Next steps" pointing to messaging patterns but says nothing about how to test handlers. Even a single sentence pointing to a testing page (or the testing page URL, even if not yet written) would help developers who are trying to write unit tests for their handlers.

**Lower priority - consider linking to EIP patterns:**

The [Event-Driven Consumer](https://www.enterpriseintegrationpatterns.com/patterns/messaging/EventDrivenConsumer.html) and [Service Activator](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessagingAdapter.html) EIP pages are the canonical academic references for what Mocha's handlers implement. Linking them in a "Further reading" section or in the architecture section would give the documentation intellectual grounding without cluttering the practical content. The [Competing Consumers](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CompetingConsumers.html) pattern is worth linking when explaining why multiple consumer instances run against the same queue under load.

Sources:

- [Handlers - NServiceBus - Particular Docs](https://docs.particular.net/nservicebus/handlers/)
- [Consumers - MassTransit](https://masstransit.io/documentation/concepts/consumers)
- [Message Handlers - Wolverine](https://wolverinefx.net/guide/handlers/)
- [Message Handler Discovery - Wolverine](https://wolverinefx.net/guide/handlers/discovery)
- [Cascading Messages - Wolverine](https://wolverinefx.net/guide/handlers/cascading.html)
- [Job Consumers - MassTransit](https://masstransit.io/documentation/patterns/job-consumers)
- [Child containers - NServiceBus](https://docs.particular.net/nservicebus/dependency-injection/child-lifetime)
- [Handler Ordering - NServiceBus](https://docs.particular.net/nservicebus/handlers/handler-ordering)
- [Async Handlers - NServiceBus](https://docs.particular.net/nservicebus/handlers/async-handlers)
- [Messages, events, and commands - NServiceBus](https://docs.particular.net/nservicebus/messaging/messages-events-commands)
- [Publish and Handle an Event - NServiceBus](https://docs.particular.net/nservicebus/messaging/publish-subscribe/publish-handle-event)
- [Message Endpoint - Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageEndpoint.html)
- [Event-Driven Consumer - Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/EventDrivenConsumer.html)
- [Competing Consumers - Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CompetingConsumers.html)
- [Service Activator - Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessagingAdapter.html)
- [Messaging Endpoints Introduction - Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessagingEndpointsIntro.html)
- [CQRS Pattern - Azure Architecture Center](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
