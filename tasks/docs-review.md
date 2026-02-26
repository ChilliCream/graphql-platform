# Mocha Documentation Review

This document is the single source of truth for rewriting the Mocha messaging bus documentation. It synthesizes 12 research reports (competitor analysis against NServiceBus, MassTransit, and Wolverine), the current 14 documentation pages, the docs-writer guidelines, and critical feedback from the project owner.

The docs-writer agent should treat each per-page review as a complete specification. Every instruction is actionable. No further clarification should be needed.

---

## Part 1: Overall Structure Review

### Current Page Order (in docs.json sidebar)

1. Introduction (index.md)
2. Quick Start (quick-start.md)
3. Handlers and Consumers (handlers-and-consumers.md)
4. Messages (messages.md)
5. Messaging Patterns (messaging-patterns.md)
6. Routing and Endpoints (routing-and-endpoints.md)
7. Middleware and Pipelines (middleware-and-pipelines.md)
8. Observability (observability.md)
9. Reliability (reliability.md)
10. Sagas (sagas.md)
11. Testing (testing.md)
12. Transports Overview (transports/index.md)
13. InMemory Transport (transports/in-memory.md)
14. RabbitMQ Transport (transports/rabbitmq.md)

### Recommended Page Order

The current order has structural problems. Handlers appear before Messages and Messaging Patterns, but a reader cannot understand what a handler does without first knowing what a message is and what patterns exist. Routing appears after patterns but before middleware, yet routing is implemented as middleware. The transport pages appear at the end, but the quick start already uses a transport.

Recommended order:

1. **Introduction** (index.md) -- What Mocha is, who it is for, core concepts
2. **Quick Start** (quick-start.md) -- Zero to working app
3. **Messages** (messages.md) -- What flows through the system
4. **Messaging Patterns** (messaging-patterns.md) -- The three patterns (events, commands, request/reply)
5. **Handlers and Consumers** (handlers-and-consumers.md) -- How you process messages
6. **Routing and Endpoints** (routing-and-endpoints.md) -- How messages find their destination
7. **Middleware and Pipelines** (middleware-and-pipelines.md) -- How to customize the processing pipeline
8. **Reliability** (reliability.md) -- Fault handling, dead-letter, outbox
9. **Observability** (observability.md) -- Tracing, metrics, diagnostics
10. **Sagas** (sagas.md) -- Long-running orchestration
11. **Testing** (testing.md) -- How to test message-driven code
12. **Transports Overview** (transports/index.md) -- Transport abstraction concept
13. **InMemory Transport** (transports/in-memory.md) -- Development and testing transport
14. **RabbitMQ Transport** (transports/rabbitmq.md) -- Production transport

**Rationale for changes:**

- Messages before Handlers: you must understand what a message is before you can understand what processes it.
- Messaging Patterns before Handlers: the pattern (event, command, request/reply) determines which handler interface to implement.
- Reliability before Observability: reliability is a more fundamental concern (fault handling, dead-letter) than instrumentation. Observability instruments what reliability builds.
- This order follows a natural learning progression: what is it -> try it -> understand the data -> understand the patterns -> write handlers -> configure routing -> customize pipelines -> handle failures -> observe the system -> orchestrate workflows -> test everything -> understand transports.

### Cross-Cutting Themes to Weave Throughout

The project owner's critical feedback identified that architecture concepts must be woven naturally into pages where they first matter, not siloed on their own pages. The following concepts should appear on multiple pages, introduced when the reader first needs them:

**1. "Everything is a pipeline"**

- First introduce on the Handlers page: "When a message arrives, it passes through a pipeline of middleware before reaching your handler."
- Reinforce on Reliability: "The circuit breaker, dead-letter routing, and concurrency limiter are all middleware in the receive pipeline."
- Reinforce on Observability: "Tracing and metrics are injected by middleware in all three pipelines."
- The standalone Middleware page then becomes "How to write your own middleware" rather than "What middleware is."

**2. Middleware as the implementation of features**

- Every feature page (Reliability, Observability) should name the specific middleware that implements the feature and link back to the Middleware page for customization.
- The Middleware page should link forward to Reliability and Observability as examples of what built-in middleware does.

**3. Scope precedence (bus > transport > endpoint)**

- First introduce on Routing: "Configuration follows a scope hierarchy: bus-level settings apply to all transports, transport-level settings apply to all endpoints on that transport, and endpoint-level settings override both."
- Reinforce on Middleware: "Middleware can be registered at bus, transport, or endpoint scope."
- Do NOT repeat the full explanation on every page. State it once on Routing and cross-reference.

**4. The handler-first design philosophy**

- Introduce on the Introduction page: Mocha is handler-first -- you write a handler, register it, and the framework builds the endpoint, route, and pipeline around it.
- Reinforce on Handlers: explain the design decision (opinionated handlers with typed interfaces vs. the `IConsumer<T>` approach).
- Reference on Routing: "Mocha derives endpoints from your handler registrations."

**5. External references (EIP, Fowler, Microsoft Architecture Center)**

- Link to the relevant Enterprise Integration Pattern on the page where the pattern is first used. Do not dump all links on one page.
- Specific assignments are listed in each per-page review below.

### Narrative Arc

The documentation should tell a story across pages. Each page ends with a bridge to the next:

- Introduction: "Ready to build? Start with the Quick Start."
- Quick Start: "Now that you have a working app, learn how messages work in Messages."
- Messages: "Now that you understand message structure, learn the three messaging patterns."
- Messaging Patterns: "Ready to implement these patterns? See Handlers and Consumers."
- Handlers: "Your handlers are registered. Learn how Mocha routes messages to them in Routing and Endpoints."
- Routing: "Want to customize the processing pipeline? See Middleware and Pipelines."
- Middleware: "The pipeline handles failures automatically. Learn how in Reliability."
- Reliability: "To monitor your messaging system, see Observability."
- Observability: "For long-running workflows that span multiple messages, see Sagas."
- Sagas: "Learn how to test all of this in Testing."
- Testing: "To understand the transport layer, see Transports."

---

## Part 2: Per-Page Review

### Page 1: Introduction (index.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/index.md` (196 lines)

**Current problems:**

1. Structural repetition: "Problems Mocha solves," "Why Mocha," and "Key capabilities" cover the same ground three times. Inter-service communication, event-driven messaging, and request/reply appear in all three sections.
2. No terminology section. The page uses "endpoint," "transport," "consumer," "handler," "fan-out," and "pipeline" without defining them. Wolverine's terminology section is the gold standard among competitors.
3. No architecture diagram. All three competitors provide a visual model of how a message flows through the system.
4. The competitive comparison sentence ("Mocha combines the developer experience of Wolverine, the configurability of MassTransit, and the observability of NServiceBus") is an unsupported marketing claim in documentation.
5. The 20-line saga code example is too complex for an introduction. Wolverine and MassTransit both defer saga internals.
6. No "learning paths" offering multiple entry points.

**What to remove:**

- The competitive comparison sentence. Replace with a neutral statement of what Mocha is optimized for.
- The full saga code block. Replace with 1-2 sentences describing that sagas exist and a link to the Sagas page.
- The "Problems Mocha solves" section. Merge its content into a unified section.
- The "Why Mocha" section as a standalone section. Fold its unique content (compiled pipelines, Nitro integration) into the unified capabilities section.

**What to add:**

1. A terminology box near the top defining: Message, Event, Command, Request, Handler, Endpoint, Transport, Pipeline, Saga. Even a compact table. (Research report 01 identifies this as the highest-impact addition.)
2. A simple architecture diagram (Mermaid) showing: `Publisher -> Bus -> Transport -> Broker -> Consumer -> Handler`. This answers "what happens when I call PublishAsync?" -- the implicit first question every new developer has.
3. A "Learning paths" section at the bottom offering three entry points: Quick Start (for builders), Messages + Patterns (for conceptual learners), Transports (for evaluators).
4. An EIP link in the opening paragraph when introducing messaging as a discipline: [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/Introduction.html).

**What to restructure:**

- Consolidate into: Opening hook (code) -> What Mocha is (1 paragraph) -> Terminology -> Architecture diagram -> Core capabilities (one section, code examples inline, saga reduced to 1-2 sentences) -> When not to use Mocha -> Learning paths.

**Specific external references to include:**

- [EIP Introduction to Messaging](https://www.enterpriseintegrationpatterns.com/patterns/messaging/Introduction.html)
- [Microsoft Azure Architecture: Event-Driven Architecture](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven)

**Target tone:** Confident, welcoming, zero marketing. A smart colleague explaining what Mocha is at a whiteboard.

---

### Page 2: Quick Start (quick-start.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/quick-start.md` (194 lines)

**Current problems:**

1. The `(MessagingRuntime)` cast pattern is a code smell that leaks implementation details. Every test file also uses this pattern.
2. The description claims "about 40 lines of code" but the actual code spans ~80 lines across three files.
3. No "what just happened?" explanation after verification. The reader sees a log line but does not understand the execution path.
4. The manual handler registration (`AddEventHandler<OrderPlacedHandler>()`) is verbose relative to competitors. The page does not acknowledge this or mention assembly-scanning alternatives.
5. The curl command uses a hardcoded port that may not match the reader's environment.

**What to remove:**

- The troubleshooting section. (Per project owner directive: remove all troubleshooting sections.)
- The "40 lines" claim from the description. Replace with an accurate description.

**What to add:**

1. A "What just happened?" paragraph after the verification step. 3-4 sentences explaining: POST hit the endpoint, the endpoint called `PublishAsync`, the bus routed the event to the handler via the InMemory transport, and the handler was invoked. This reinforces the mental model.
2. A brief message-flow visualization at the top (even text-based): `POST /orders -> PublishAsync(OrderPlaced) -> InMemory Bus -> OrderPlacedHandler`.
3. A note about the port: "Check your console output for the actual URL. ASP.NET Core's default port may differ."
4. More opinionated "Next steps" following NServiceBus's model: "Want to understand messaging patterns? Read Messages. Ready for production? See Transports."

**What to restructure:**

- The `runtime.StartAsync` pattern: if the API cannot be changed, wrap it in a helper or extension method in the example and add a comment explaining why it is needed. The raw cast should not be the first thing a new user sees.
- Lead with the outcome: "By the end of this guide, you will have an ASP.NET Core app that publishes an OrderPlaced event and handles it -- all running in-process with the InMemory transport."

**Specific external references to include:**

- None needed for a quick start. Keep it focused on getting to "it works."

**Target tone:** Encouraging, step-by-step, zero theory. The reader should feel accomplishment.

---

### Page 3: Messages (messages.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/messages.md` (421 lines)

**Current problems:**

1. Three heavy API reference tables (`MessageEnvelope` with 16 properties, `IHeaders`/`IReadOnlyHeaders`, `IConsumeContext<T>` with 20 properties) consume ~40% of the page. Per project owner: remove API reference tables.
2. "Why envelopes exist" appears at position 8 of 11 sections. It should be near the top -- the rationale makes the tutorial meaningful.
3. The page conflates message design (what goes in the POCO) with wire format (how the envelope wraps it). These are conceptually separate concerns.
4. No naming convention guidance. The examples follow verb-noun commands / noun-past-tense events but never state this as a rule. Both NServiceBus and MassTransit document this explicitly.
5. No message versioning guidance. All competitors have gaps here, but Wolverine's `[MessageIdentity]` + `IForwardsTo<T>` pattern shows it is achievable.
6. The `IEventHandler<T>` vs `IConsumer<T>` distinction is explained twice on this page (tutorial section and "Access envelope metadata" section).
7. No serialization default stated. The page mentions `ContentType` in passing but never says "JSON by default."

**What to remove:**

- The `MessageEnvelope` reference table (16 properties). This is API reference, not conceptual documentation.
- The `IHeaders` / `IReadOnlyHeaders` / `HeaderValue` reference tables. Same reason.
- The `IConsumeContext<T>` reference table (20 properties). Same reason.
- The troubleshooting section. (Per project owner directive.)
- The duplicate `IEventHandler<T>` vs `IConsumer<T>` explanation. Keep one, remove the other.

**What to add:**

1. Move "Why envelopes exist" to position 2, right after the opening paragraph. The rationale anchors the tutorial.
2. A "Naming conventions" callout: commands use imperative verb-noun form (`PlaceOrder`, `ProcessPayment`), events use past-tense noun-verb form (`OrderPlaced`, `PaymentProcessed`). One sentence or a callout box. (Research report 04 identifies this as high-value.)
3. A brief "Message versioning" section: adding new `init` properties with defaults is backward-compatible, renaming or removing properties is breaking, `AddMessage<T>()` pins the URN when refactoring namespaces. Even two paragraphs puts Mocha ahead of every competitor.
4. One sentence on serialization: "Mocha serializes message bodies as JSON by default."
5. A principle statement inspired by MassTransit: "Your POCO contains business data; the envelope contains infrastructure metadata. Keep them separate."
6. Link to EIP [Message pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/Message.html) when introducing the header/body separation concept.

**What to restructure:**

- Order: Opening paragraph -> Why envelopes exist -> Naming conventions -> Tutorial (define POCO, set headers, access metadata) -> Correlation (ConversationId/CorrelationId/CausationId diagram) -> Message type resolution -> Message versioning -> Next steps.
- The correlation ASCII diagram is an asset. Keep it.

**Specific external references to include:**

- [EIP Message pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/Message.html)
- [CloudEvents spec](https://github.com/cloudevents/spec/blob/main/cloudevents/spec.md) -- mention as the industry-standard envelope format for context.

**Target tone:** Conceptual first, practical second. Teach the mental model of envelope vs. payload, then show code.

---

### Page 4: Messaging Patterns (messaging-patterns.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/messaging-patterns.md` (633 lines)

**Current problems:**

1. The page opens with the comparison table immediately, with no architectural framing. The table is useful as a reference but is a poor first encounter with the patterns.
2. No EIP links for any of the three patterns. All three competitors link to EIP or other authoritative sources.
3. No explanation of the "command in disguise" anti-pattern -- the most common messaging architecture mistake (using `PublishAsync` for a message only one service should handle).
4. No transport-cost awareness for publish vs send. MassTransit documents that publishing commands incurs overhead on some transports.
5. The mechanical underpinning of request/reply (correlation ID, reply queue) is not explained. The page shows the API but not what happens at the transport level.
6. `PublishOptions`, `SendOptions`, and `RequestAsync` API reference tables add weight. Per project owner: remove API reference tables.

**What to remove:**

- The `PublishOptions` reference table. Remove.
- The `SendOptions` reference table. Remove.
- The `RequestOptions` / `RequestAsync` overload table. Remove.
- The troubleshooting section. (Per project owner directive.)

**What to add:**

1. An architectural framing introduction (2 paragraphs) before the table. Explain that each pattern answers a different question: "Who needs to know?" (events), "Who should act?" (commands), "What is the result?" (request/reply). Link to Martin Fowler's ["What do you mean by Event-Driven?"](https://martinfowler.com/articles/201701-event-driven.html) for the definitive taxonomy.
2. EIP links for each pattern's first mention:
   - Events: [Publish-Subscribe Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html)
   - Commands: [Command Message](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CommandMessage.html)
   - Request/Reply: [Request-Reply](https://www.enterpriseintegrationpatterns.com/patterns/messaging/RequestReply.html)
3. The command/event naming convention as explicit guidance: commands = verb-noun present tense, events = noun-verb past tense. Both NServiceBus and MassTransit document this.
4. A "command in disguise" anti-pattern callout: "If your 'event' expects exactly one consumer to take action, it is a command. Use `SendAsync`, not `PublishAsync`."
5. A brief paragraph explaining request/reply mechanics: Mocha creates a temporary reply address, embeds it in the request envelope, and the handler sends the response back to that address. This demystifies `ResponseTimeoutException`.
6. A "See also" section at the bottom with curated external links.

**What to restructure:**

- Order: Architectural framing -> Comparison table -> Events (with EIP link, code, "how to" example) -> Commands (with EIP link, code, anti-pattern callout) -> Request/Reply (with EIP link, code, mechanics explanation) -> When to use which pattern (decision table) -> See also.
- Each pattern section should follow: 1-2 sentence concept, EIP link, code example, secondary example.

**Specific external references to include:**

- [EIP Publish-Subscribe Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html)
- [EIP Command Message](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CommandMessage.html)
- [EIP Request-Reply](https://www.enterpriseintegrationpatterns.com/patterns/messaging/RequestReply.html)
- [Martin Fowler: "What do you mean by Event-Driven?"](https://martinfowler.com/articles/201701-event-driven.html)
- [Microsoft Azure Architecture: Publisher-Subscriber Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/publisher-subscriber)
- [CodeOpinion: Commands & Events](https://codeopinion.com/commands-events-whats-the-difference/)

**Target tone:** Architectural and principled. This page teaches design thinking, not just API usage.

---

### Page 5: Handlers and Consumers (handlers-and-consumers.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/handlers-and-consumers.md` (585 lines)

**Current problems:**

1. No "when to use which handler" orientation before the first code block. The page jumps to `IEventHandler<T>` without explaining why three handler interfaces exist.
2. Consumer type adapters (`SubscribeConsumer`, `RequestConsumer`, `SendConsumer`, `BatchConsumer`, `ConsumerAdapter`) are documented. Per project owner: consumer type adapters are internal implementation details -- do NOT document them.
3. The `IHandler` base interface and its static abstract members are exposed. This is internal implementation. Remove.
4. The `IMessageBatch<T>` reference table and `BatchOptions` reference table are API reference. Per project owner: remove API reference tables.
5. No explanation of DI scoping (Mocha creates a new scope per message). MassTransit documents this on their consumers page.
6. No explanation of what happens when a handler throws.
7. No example of publishing from within a handler.
8. The `IConsumeContext<T>` properties are listed as a prose sentence, not a scannable format.

**What to remove:**

- The entire "Architecture: consumers and the handler pipeline" section that documents consumer adapter mapping (`SubscribeConsumer`, `RequestConsumer`, etc.), the `IHandler` base interface, and consumer adapter internals. (Per project owner directive.)
- The `IMessageBatch<T>` reference table. Remove.
- The `BatchOptions` reference table. Remove.
- The full interface signatures table listing every handler interface with every method signature. Remove.
- The troubleshooting section. (Per project owner directive.)

**What to add:**

1. A "When to use which handler" orientation paragraph before the first code block. Map handler type to intent:
   - `IEventHandler<T>`: react to a published event. No reply expected. Multiple handlers can receive it.
   - `IEventRequestHandler<TReq, TRes>`: handle a request and return a typed response.
   - `IEventRequestHandler<TReq>`: handle a command with no response.
   - `IBatchEventHandler<T>`: process multiple events at once for efficiency.
   - `IConsumer<T>`: access raw envelope metadata. Use when you need headers, correlation IDs, or the consume context.
2. A DI scoping explanation: "Mocha creates a new DI scope for each message. Constructor dependencies are resolved from that scope. `DbContext` and other scoped services are safe to inject directly."
3. An exception behavior explanation: what happens when `HandleAsync` throws? Is the message retried? How does the exception surface in request/reply mode? Link to Reliability for retry/fault behavior.
4. A "Publishing from within a handler" example: inject `IMessageBus` via constructor, call `PublishAsync`. Clarify whether the published message inherits `CorrelationId` and `ConversationId` from the inbound message.
5. A brief pipeline introduction: "When a message arrives, it passes through a pipeline of middleware before reaching your handler. The pipeline handles fault routing, dead-letter, observability, and concurrency. See Middleware and Pipelines for details."
6. EIP links in a "Further reading" note:
   - [Event-Driven Consumer](https://www.enterpriseintegrationpatterns.com/patterns/messaging/EventDrivenConsumer.html)
   - [Competing Consumers](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CompetingConsumers.html)

**What to restructure:**

- Order: When to use which handler -> Event handler tutorial (with verification) -> Request handler -> Command handler -> Batch handler -> IConsumer (as "advanced: accessing the envelope") -> DI scoping -> Exception behavior -> Publishing from a handler -> Next steps.

**Target tone:** Practical, tutorial-led. Show working code first, explain after.

---

### Page 6: Routing and Endpoints (routing-and-endpoints.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/routing-and-endpoints.md` (543 lines)

**Current problems:**

1. No conceptual opening paragraph. The page jumps into code without establishing what an "endpoint" is.
2. The difference between logical and physical endpoints is never named. NServiceBus makes this a central pillar.
3. No visual diagram showing the flow from handler registration to endpoint to queue.
4. "Why does the service prefix exist for subscribe routes but not send routes?" is documented but never explained. The answer (fan-out vs point-to-point) maps to EIP patterns.
5. Heavy reference tables: `IReceiveEndpointDescriptor`, `IDispatchEndpointDescriptor`, `IBusNamingConventions`, `ReceiveEndpointConfiguration`, `DispatchEndpointConfiguration`. Per project owner: remove API reference tables.
6. The six-phase initialization walkthrough describes what happens but not why it matters (topology errors surface at startup, not at send time).
7. No routing priority rule. Wolverine's five-rule priority hierarchy is their most useful routing documentation feature.

**What to remove:**

- The `IReceiveEndpointDescriptor` reference table. Remove.
- The `IDispatchEndpointDescriptor` reference table. Remove.
- The `IBusNamingConventions` method table. Remove.
- The `ReceiveEndpointConfiguration` and `DispatchEndpointConfiguration` full property tables. Remove.
- The convention type reference table. Remove.
- The troubleshooting section. (Per project owner directive.)

**What to add:**

1. A two-sentence conceptual opening: "An endpoint is the combination of a transport address (a queue or exchange) and a pipeline that processes messages. Mocha distinguishes between receive endpoints (which consume) and dispatch endpoints (which produce)."
2. A "Why the service prefix?" callout: events use fan-out delivery via exchanges -- each subscribing service needs its own queue. Without a service-specific prefix, two services would compete on the same queue. Link to EIP [Point-to-Point Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PointToPointChannel.html).
3. A routing priority rule: "(1) If an explicit outbound route is registered with `AddMessage<T>()`, use it. (2) Otherwise, derive the endpoint name from naming conventions."
4. A note about startup-time topology: "Topology errors surface at startup, not at the first `SendAsync` call. If your exchange or queue configuration is invalid, you will know immediately."
5. Scope precedence explanation: "Configuration follows a hierarchy: bus > transport > endpoint." Show one example. This becomes the canonical explanation other pages cross-reference.
6. EIP references:
   - [Message Endpoint](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageEndpoint.html)
   - [Point-to-Point Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PointToPointChannel.html)

**What to restructure:**

- Order: Conceptual opening -> The endpoint model (receive vs dispatch, with a simple diagram) -> How routing works (inbound vs outbound, priority rule) -> Naming conventions (with "why the service prefix?" callout) -> The `Describe()` verification tool -> Customize outbound routes -> Bind consumers to endpoints -> Scope precedence -> Next steps.
- The `Describe()` method verification section is a genuine differentiator. Keep and promote it.

**Target tone:** Conceptual clarity first, then configuration. Explain "why" before "how."

---

### Page 7: Middleware and Pipelines (middleware-and-pipelines.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/middleware-and-pipelines.md` (391 lines)

**Current problems:**

1. The ASP.NET Core analogy is stated but never visualized. No nesting/wrapping ("onion") diagram.
2. The `Create()` factory pattern is shown before its purpose is explained. The explanation (DI lifetime scoping) appears 200 lines later in the compiler section.
3. The built-in middleware catalog lists every middleware but does not explain which ones implement which features. "CircuitBreaker" is listed but not connected to the Reliability page.
4. The compiler internals section is interesting but is internal implementation detail that adds page weight for most readers.
5. Consumer middleware is shown third even though it is the most common use case (wrapping handler execution).

**What to remove:**

- The troubleshooting section. (Per project owner directive.)
- The built-in middleware key reference table. This is implementation detail.
- The compiler internals section ("How the middleware compiler works"). This is implementation detail for most readers. If retained, collapse it into a brief note.

**What to add:**

1. A nesting/wrapping diagram for a single pipeline. Show the "onion" model: middleware A wraps B wraps C. Exceptions propagate back out. This is the mental model developers need. Link to [ASP.NET Core Middleware docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/) for the canonical diagram.
2. The factory pattern explanation BEFORE the first middleware example: "The factory lambda runs once per message and resolves services from the request-scoped container. If you capture a service outside the lambda, it behaves as a singleton across all messages -- which breaks scoped services like `DbContext`."
3. A "Which pipeline should I target?" decision table:
   - Add a header to every outgoing message: Dispatch
   - Validate messages before sending: Dispatch
   - Rate-limit incoming messages: Receive
   - Wrap every handler in a database transaction: Consumer
   - Time individual handler execution: Consumer
4. Cross-references to feature pages: "The CircuitBreaker middleware implements the circuit breaker described in Reliability. The ReceiveInstrumentation middleware generates the OpenTelemetry spans described in Observability."
5. A real-world consumer middleware example (database unit-of-work) instead of or alongside the timing example. NServiceBus's unit-of-work sample is the gold standard.
6. Anchor to the named pattern: "Mocha's pipeline implements the [Pipes and Filters](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PipesAndFilters.html) pattern from Enterprise Integration Patterns."

**What to restructure:**

- Order: Opening with pattern anchor and ASP.NET Core analogy -> Nesting diagram -> The three pipelines (overview diagram) -> "Which pipeline should I target?" table -> Consumer middleware example (most common) -> Receive middleware example -> Dispatch middleware example -> Factory pattern explanation -> Cross-references to Reliability and Observability -> Next steps.
- Start with consumer middleware (most common use case), not dispatch.

**Specific external references to include:**

- [EIP Pipes and Filters](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PipesAndFilters.html)
- [ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)

**Target tone:** Practical with architecture grounding. The reader should understand the mental model (wrapping/nesting) and know immediately which pipeline to target.

---

### Page 8: Reliability (reliability.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/reliability.md` (405 lines)

**Current problems:**

1. The dual-write problem is named but failure modes are abstract. NServiceBus names them concretely ("zombie records" and "ghost messages").
2. No outbox flow diagram. Every competitor uses a visual for this.
3. No mention of idempotency or inbox pattern. The outbox guarantees at-least-once delivery, meaning handlers may be invoked more than once. This is never stated.
4. `ConcurrencyLimiterOptions` and `CircuitBreakerOptions` reference tables are API reference. Per project owner: remove.
5. No delivery guarantees comparison (without outbox vs. with outbox).
6. The page does not connect features to the pipeline. The circuit breaker, dead-letter, and concurrency limiter are all middleware in the receive pipeline, but the page never says this.

**What to remove:**

- The `ConcurrencyLimiterOptions` reference table. Remove.
- The `CircuitBreakerOptions` reference table. Remove.
- The troubleshooting section. (Per project owner directive.)

**What to add:**

1. Concrete failure mode names in the dual-write section: "If the database commits but the publish fails, the event is lost -- downstream consumers never see it. If the publish succeeds but the database rolls back, the event describes state that never existed."
2. A two-phase outbox diagram (Mermaid sequence diagram): `Handler -> Outbox Table (same transaction) -> Outbox Processor -> Transport`.
3. An idempotency callout after the outbox section: "The outbox guarantees at-least-once delivery. Your handlers may be invoked more than once for the same message. Design handlers to be idempotent." Link to [Idempotent Consumer pattern](https://microservices.io/patterns/communication-style/idempotent-consumer.html).
4. A delivery guarantees comparison: "Without outbox: at-most-once publish. With outbox: at-least-once publish."
5. Pipeline connection sentences: "The circuit breaker is implemented as the `CircuitBreaker` middleware in the receive pipeline. See Middleware and Pipelines for positioning and customization."
6. EIP references:
   - [Guaranteed Delivery](https://www.enterpriseintegrationpatterns.com/patterns/messaging/GuaranteedMessaging.html)
   - [Dead Letter Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/DeadLetterChannel.html)
   - [Transactional Outbox](https://microservices.io/patterns/data/transactional-outbox.html) (Chris Richardson)

**What to restructure:**

- Order: Opening framing -> Delivery guarantees comparison -> Fault handling (with pipeline reference) -> Dead-letter routing -> Message expiry -> Concurrency limiting (brief, no reference table) -> Circuit breaker (brief, no reference table) -> Transactional outbox (with diagram, failure mode names, idempotency callout) -> Next steps.

**Target tone:** Direct and operationally focused. This page should make the reader feel confident about production deployment.

---

### Page 9: Observability (observability.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/observability.md` (426 lines)

**Current problems:**

1. Seven reference subsections (ActivitySource, Span attributes, Metrics, Queue metrics, Topic metrics, Metric dimensions, Trace headers, `IBusDiagnosticObserver` interface, Registration method) add massive weight. Per project owner: remove API reference tables.
2. The `IBusDiagnosticObserver` full interface listing (16 lines of C#) belongs in API reference, not conceptual documentation.
3. Custom non-W3C headers (`trace-id`, `span-id`, `trace-state`, `parent-id`) are presented as if standard. The actual code uses Mocha-specific headers, not W3C `traceparent`.
4. Non-standard span attributes (`messaging.handler.name`, `messaging.instance.id`, `messaging.message.type`) are presented as OTel conventions but are not in the official spec.
5. The Aspire configuration block (25 lines) interrupts the conceptual flow between trace propagation and custom observers.
6. No "what you will see" visual or description. Competitors with Jaeger screenshots have a significant usability advantage.
7. No log-trace correlation note.

**What to remove:**

- The `ActivitySource` and `Meter` reference table. Remove.
- The span attributes reference table. Remove.
- The metrics tables (Metrics, Queue metrics, Topic metrics). Remove.
- The metric dimensions reference table. Remove.
- The trace propagation headers reference table. Remove.
- The `IBusDiagnosticObserver` full interface listing. Remove. (The custom observer example can stay as a how-to, just without the full interface reference.)
- The registration method table. Remove.
- The troubleshooting section. (Per project owner directive.)

**What to add:**

1. A note acknowledging the custom header format: "Mocha uses Mocha-specific headers (`trace-id`, `span-id`, `trace-state`, `parent-id`) rather than the W3C `traceparent` format. When receiving messages from non-Mocha producers, the receive middleware starts a new root trace."
2. A "what you will see" description: "In the Aspire Dashboard (or Jaeger), you will see a `publish` span from the publishing service linked to a `receive` span in the consumer service, with a child `consumer` span for the handler execution."
3. A log-trace correlation note: "When OpenTelemetry is configured with `AddOpenTelemetry().WithLogging()`, your `ILogger` structured logs automatically include `TraceId` and `SpanId` fields, enabling correlation between logs and traces."
4. Pipeline connection: "Tracing and metrics are injected by middleware in all three pipelines: `ReceiveInstrumentation`, `DispatchInstrumentation`, and `ConsumerInstrumentation`. See Middleware and Pipelines."
5. OTel semconv stability note: "Mocha follows the OpenTelemetry messaging semantic conventions, which are currently in development status."

**What to restructure:**

- Order: Opening paragraph (opt-in, no-op default) -> Enable tracing and metrics (code) -> What you will see (description) -> How trace context propagates (Mermaid diagram, with header format note) -> Custom diagnostic observer (how-to, without full interface listing) -> Aspire configuration (moved later, as an integration scenario) -> Log-trace correlation -> Next steps.
- The Mermaid sequence diagram is an asset. Keep it.

**Specific external references to include:**

- [OpenTelemetry Messaging Spans](https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [.NET Distributed Tracing Concepts](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-concepts)

**Target tone:** Operational and grounded. Focus on "here is how to set it up and what you will see," not on exhaustive reference tables.

---

### Page 10: Sagas (sagas.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/sagas.md` (669 lines)

**Current problems:**

1. No state machine diagram. No competitor does this either, which means adding one would make Mocha the clear leader.
2. No theoretical grounding. The saga pattern originates from Garcia-Molina & Salem (1987). MassTransit cites the original paper. NServiceBus references the Process Manager pattern from Hohpe & Woolf.
3. No mention that messages can arrive out of order. NServiceBus's tutorial treats this as a key teaching moment.
4. Full API reference tables: `ISagaDescriptor`, `ISagaStateDescriptor`, `ISagaTransitionDescriptor`, `ISagaFinalStateDescriptor`, `SagaStateBase`, `ISagaStore`, `ISagaTransaction`. Per project owner: remove.
5. The "When to use sagas vs. handlers" table is a genuine strength but is buried near the end.
6. Timeouts are barely covered (one line in the reference table).
7. Compensation is shown mechanically (`OnFault()`) without explaining the concept of compensating transactions.
8. No concurrency discussion.

**What to remove:**

- The `ISagaDescriptor` reference table. Remove.
- The `ISagaStateDescriptor` reference table. Remove.
- The `ISagaTransitionDescriptor` reference table. Remove.
- The `ISagaFinalStateDescriptor` reference table. Remove.
- The `SagaStateBase` reference table. Remove.
- The `ISagaStore` reference table. Remove.
- The `ISagaTransaction` reference table. Remove.
- The troubleshooting section. (Per project owner directive.)

**What to add:**

1. A Mermaid state machine diagram for the refund saga tutorial, showing states as nodes and message-triggered transitions as labeled edges. This single addition would make Mocha's saga documentation better than any competitor's. Example:
   ```
   [Initial] --RequestQuickRefundRequest--> [AwaitingRefund] --ProcessRefundResponse--> [Completed]
   ```
2. A brief theoretical grounding paragraph (2-3 sentences): the saga pattern was introduced by Garcia-Molina & Salem in 1987 for managing long-lived transactions without distributed locks. In the messaging context, sagas implement the Process Manager pattern from Enterprise Integration Patterns -- they coordinate a sequence of messages and track state across them.
3. A "messages can arrive out of order" callout warning. If a saga handles `OrderPlaced` and `PaymentReceived`, either may arrive first. Both must be configured as initial transitions.
4. A compensating transactions concept section before the "Handle faults and compensation" how-to. Explain what compensating transactions are and why they are needed in systems without distributed ACID.
5. A timeout how-to guide: show `OnTimeout()` with scheduling, cancellation, and what to do when a timeout fires.
6. A concurrency note: "If two messages for the same saga arrive simultaneously, one succeeds and the other retries via optimistic concurrency."
7. Move "When to use sagas vs. handlers" earlier -- ideally to position 2 (after opening paragraph, before the tutorial).

**What to restructure:**

- Order: Opening paragraph with theoretical grounding -> When to use sagas vs. handlers (moved from end) -> State machine diagram -> Tutorial: Build a refund saga -> How-to: Parallel operations -> How-to: Event-initiated sagas -> How-to: Compensation (with conceptual intro) -> How-to: Timeouts -> How-to: Persistence -> Concurrency note -> Next steps.

**Specific external references to include:**

- [microservices.io Saga pattern](https://microservices.io/patterns/data/saga.html)
- [EIP Process Manager](https://www.enterpriseintegrationpatterns.com/patterns/messaging/ProcessManager.html)
- [Microsoft Azure Architecture: Saga pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/saga)

**Target tone:** Pattern-grounded and tutorial-led. The reader should understand why sagas exist before writing one.

---

### Page 11: Testing (testing.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/testing.md` (569 lines)

**Current problems:**

1. The `MessageRecorder` helper must be implemented by the user. Every competitor ships a built-in test harness.
2. The `(MessagingRuntime)` cast appears in every test example.
3. No testing philosophy or pyramid. The page shows only integration tests. No guidance on when to prefer unit testing handler logic vs. full integration testing.
4. No guidance on testing error handling (what happens when a handler throws, how to test dead-letter scenarios).
5. The `SagaTester<T>` section shows only single-event transitions, not multi-event scenarios.
6. No `IClassFixture` or `CollectionFixture` guidance for xUnit.

**What to remove:**

- The troubleshooting section. (Per project owner directive.)

**What to add:**

1. A testing philosophy introduction: "When to use each approach" -- direct handler invocation for decision logic, InMemory transport for routing/middleware/flow, real broker for transport-specific behavior. Reference [Martin Fowler's Practical Test Pyramid](https://martinfowler.com/articles/practical-test-pyramid.html).
2. A "Unit testing handler logic" section showing direct handler invocation without standing up any transport. 5 lines instead of 15.
3. If `MessageRecorder` cannot be built into the framework, provide it as a complete, copy-paste utility class at the start of the page that users install once per test project.
4. Hide the `MessagingRuntime` cast inside the `TestBus` fixture. The fixture code should be promoted to the top of the page as the recommended pattern. Individual test examples should use the fixture, not raw DI setup.
5. A section on testing error handling: what happens when a handler throws? How to assert on `RemoteErrorException`?
6. Expanded `SagaTester<T>` example showing a multi-event sequence and outbound message assertions.
7. xUnit `IClassFixture` guidance for shared bus setup.

**What to restructure:**

- Order: Testing philosophy -> `TestBus` fixture (canonical setup, promoted from bottom to top) -> Unit testing handler logic (direct invocation) -> Integration: publish and assert -> Integration: request/reply -> Integration: send/fire-and-forget -> Integration: fan-out -> Integration: custom headers -> Saga testing (multi-event) -> Error handling testing -> xUnit fixture guidance -> Next steps.

**Specific external references to include:**

- [Martin Fowler: Practical Test Pyramid](https://martinfowler.com/articles/practical-test-pyramid.html)
- [Martin Fowler: Testing Strategies in a Microservice Architecture](https://martinfowler.com/articles/microservice-testing/)

**Target tone:** Practical and confidence-building. Tests should be easy to write and reliable.

---

### Page 12: Transports Overview (transports/index.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/transports/index.md` (201 lines)

**Current problems:**

1. The transport abstraction concept is mentioned in one sentence but not explained. Why does this pattern exist? What benefit does portability give?
2. The two-connection model (one for consuming, one for dispatching) is mentioned but not explained. The reason (back-pressure isolation) is never stated.
3. The decision matrix shows features but not trade-offs. NServiceBus's transport selection guide lists explicit disadvantages.

**What to remove:**

- The troubleshooting section. (Per project owner directive.)

**What to add:**

1. A conceptual paragraph: "A transport connects Mocha to a message broker. The transport abstraction means your handlers, patterns, and pipeline are the same regardless of which broker you use. Only the infrastructure changes." Link to [EIP Message Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageChannel.html).
2. The two-connection explanation: "Mocha opens two connections to the broker -- one for consuming and one for dispatching. This prevents a slow consumer from blocking outbound message publishing via back-pressure."
3. Trade-off transparency in the decision matrix: InMemory loses messages on restart, cannot scale across processes. RabbitMQ requires operational expertise, adds latency, has network partition limitations.

**What to restructure:**

- Order: Conceptual opening -> Add a transport (code) -> Decision matrix with trade-offs -> Two-connection explanation -> Transport lifecycle (Mermaid) -> Scope and middleware -> Next steps.

**Target tone:** Conceptual and honest about trade-offs.

---

### Page 13: InMemory Transport (transports/in-memory.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/transports/in-memory.md` (256 lines)

**Current problems:**

1. Reference tables for registration, transport descriptor, topology, endpoint, and middleware methods. Per project owner: remove.
2. The "not for production" constraint is buried in the "When to use" table. Should be a prominent callout.
3. The testing section does not address async timing, concurrency, or test isolation.
4. No explanation of the in-process topology model (topics, queues, bindings within a process).

**What to remove:**

- All reference tables (registration methods, transport descriptor, topology descriptor, endpoint descriptor, middleware methods). Remove.
- The troubleshooting section. (Per project owner directive.)

**What to add:**

1. Promote "not for production" to a prominent callout near the top.
2. Explain the in-process topology model in one paragraph: the InMemory transport replicates the same topic/queue/binding model as RabbitMQ within a single process. This is why InMemory can be swapped for RabbitMQ without code changes.
3. Expand the testing section with guidance on async handler completion waiting, test isolation (one `ServiceProvider` per test), and concurrency behavior.

**Target tone:** Practical, testing-focused.

---

### Page 14: RabbitMQ Transport (transports/rabbitmq.md)

**Current file:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/transports/rabbitmq.md` (346 lines)

**Current problems:**

1. Reference tables for exchange descriptor, queue descriptor, receive endpoint descriptor, middleware methods. Per project owner: remove.
2. No quorum queue recommendation. The queue descriptor shows `x-queue-type` but does not say which type to use for production.
3. No publisher confirms mention.
4. No message loss warning for unbound exchanges.
5. No connection recovery guidance.
6. The management UI is mentioned in "Verify it works" but not in troubleshooting.

**What to remove:**

- The exchange descriptor reference table. Remove.
- The queue descriptor reference table. Remove.
- The receive endpoint descriptor reference table. Remove.
- The middleware methods reference table. Remove.
- The default receive middleware table. Remove.
- The troubleshooting section. (Per project owner directive.)

**What to add:**

1. An explicit quorum queue recommendation: "For production workloads, use quorum queues. Quorum queues replicate across nodes using the Raft consensus algorithm and are the recommended queue type since RabbitMQ 4.0." Show the configuration. Link to [RabbitMQ Quorum Queues](https://www.rabbitmq.com/docs/quorum-queues).
2. A message-loss warning: "Messages published before the transport completes its Start phase may be lost if no queue is bound to the exchange yet."
3. A publisher confirms note (if applicable): state whether Mocha uses publisher confirms and what delivery guarantee users get.
4. The two-connection explanation (if not already covered on the overview page).
5. A manual connection string example alongside the Aspire example, for teams not using Aspire.

**What to restructure:**

- Order: Set up (Aspire + manual connection string) -> How topology works (Mermaid diagram) -> Quorum queue recommendation -> Declare custom topology -> Prefetch and concurrency tuning -> Auto-provisioned resource naming -> Next steps.
- The Mermaid topology diagram is the clearest visual in the transport pages. Keep and promote it.

**Specific external references to include:**

- [RabbitMQ Quorum Queues](https://www.rabbitmq.com/docs/quorum-queues)
- [RabbitMQ Reliability Guide](https://www.rabbitmq.com/docs/reliability)
- [CloudAMQP Best Practices](https://www.cloudamqp.com/blog/part1-rabbitmq-best-practice.html)

**Target tone:** Production-oriented. This page should make the reader confident about deploying with RabbitMQ.

---

## Part 3: Cross-Page Consistency

### Terminology

The following terms must be used consistently across all pages. Define on the Introduction page; use the same term everywhere:

| Term       | Definition                                                                  | Never say instead                                                                                          |
| ---------- | --------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Handler    | A class implementing a Mocha handler interface that processes a message     | "consumer" (except when specifically discussing `IConsumer<T>`)                                            |
| Consumer   | Internal wrapper that connects a handler to the pipeline                    | (Do not document this term -- per project owner, consumer adapters are internal)                           |
| Message    | A plain C# class representing business data                                 | "event class," "command class" (use the pattern name to describe intent, not the C# type)                  |
| Event      | A message published via `PublishAsync`. Represents something that happened. | "notification"                                                                                             |
| Command    | A message sent via `SendAsync`. Represents an instruction to act.           | "request" (unless it is request/reply)                                                                     |
| Endpoint   | A transport address (queue or exchange) paired with a processing pipeline   | "destination," "target"                                                                                    |
| Transport  | The infrastructure layer connecting Mocha to a message broker               | "provider," "driver"                                                                                       |
| Pipeline   | The chain of middleware that processes a message                            | "processing chain"                                                                                         |
| Middleware | A component in the pipeline that wraps the next component                   | "filter," "behavior" (those are competitor terms)                                                          |
| Envelope   | The wire-format wrapper containing headers and the message body             | "wrapper," "container"                                                                                     |
| Bus        | The central messaging runtime (`IMessageBus`)                               | "service bus," "message broker" (the bus is the in-process API; the broker is the external infrastructure) |

### Code Patterns

Every code example across all pages must follow these conventions:

1. **File-scoped namespaces.** `namespace MyApp;` not `namespace MyApp { }`.
2. **4-space indentation.** Per project code style.
3. **`CancellationToken` parameter.** Always include it in handler signatures.
4. **`IMessageBus` for publishing/sending.** Never use `IMessagingRuntime` directly in user-facing code examples (except the `TestBus` fixture where the cast is unavoidable).
5. **Records for message types.** Use `record` with `{ get; init; }` properties, not classes. This matches MassTransit's recommendation and modern C# style.
6. **Consistent scenario.** When a page needs a business scenario, use the order/e-commerce domain (OrderPlaced, ProcessRefund, etc.) that the quick start establishes. Do not introduce new domains unless the page specifically requires it (e.g., sagas can use a refund domain).
7. **No `using` statements in snippets** unless a non-obvious namespace is required. Keep examples minimal.
8. **Show expected output** after every code block that produces observable results. The reader must be able to verify they are on track.

### Recurring Concepts

These concepts appear on multiple pages. State them once canonically on the designated page and cross-reference elsewhere:

| Concept                                                  | Canonical page           | How to reference on other pages                                                                        |
| -------------------------------------------------------- | ------------------------ | ------------------------------------------------------------------------------------------------------ |
| Scope precedence (bus > transport > endpoint)            | Routing and Endpoints    | "Configuration follows the scope hierarchy described in Routing and Endpoints."                        |
| Pipeline model (three pipelines)                         | Middleware and Pipelines | "The [feature] is implemented as middleware in the [pipeline] pipeline. See Middleware and Pipelines." |
| DI scoping (new scope per message)                       | Handlers and Consumers   | "Mocha creates a new DI scope for each message, as described in Handlers and Consumers."               |
| Naming conventions                                       | Routing and Endpoints    | "Endpoint names follow the conventions described in Routing and Endpoints."                            |
| Correlation (ConversationId, CorrelationId, CausationId) | Messages                 | "Correlation IDs are propagated as described in Messages."                                             |
| Delivery guarantees                                      | Reliability              | "The outbox provides at-least-once delivery as described in Reliability."                              |

---

## Part 4: Priority Order

### Tier 1: Rewrite First (Foundation Pages)

These pages must be rewritten first because all other pages depend on concepts they introduce.

1. **Introduction (index.md)** -- Establishes terminology, architecture diagram, and the mental model every other page references. Write this first.
2. **Messages (messages.md)** -- Establishes the envelope concept, naming conventions, and correlation model that handlers, patterns, and observability all reference.
3. **Messaging Patterns (messaging-patterns.md)** -- Establishes the three patterns that handlers, routing, and sagas all reference.

### Tier 2: Rewrite Second (Core Workflow Pages)

These pages form the core developer workflow and are the most-visited after the quick start.

4. **Quick Start (quick-start.md)** -- The entry point for new users. Depends on Introduction for mental model. Can reference new terminology established in Tier 1.
5. **Handlers and Consumers (handlers-and-consumers.md)** -- The primary authoring surface. Depends on Messages and Patterns for context.
6. **Routing and Endpoints (routing-and-endpoints.md)** -- Depends on Handlers for the "handlers drive endpoints" concept.

### Tier 3: Rewrite Third (Feature Pages)

These pages describe features that build on the core workflow. They can be written in parallel.

7. **Middleware and Pipelines (middleware-and-pipelines.md)** -- Depends on Routing for scope precedence.
8. **Reliability (reliability.md)** -- Depends on Middleware for pipeline references.
9. **Observability (observability.md)** -- Depends on Middleware for pipeline references.

### Tier 4: Rewrite Last (Advanced and Transport Pages)

These pages are either advanced topics or transport-specific. They depend on all previous tiers.

10. **Sagas (sagas.md)** -- Depends on Patterns, Handlers, and Reliability.
11. **Testing (testing.md)** -- Depends on all other pages (tests exercise every feature).
12. **Transports Overview (transports/index.md)** -- Foundational for the two transport pages.
13. **InMemory Transport (transports/in-memory.md)** -- Depends on Transports Overview.
14. **RabbitMQ Transport (transports/rabbitmq.md)** -- Depends on Transports Overview.

### Quick Wins (Can Be Done During Any Tier)

These changes are small and high-impact. They can be applied during the rewrite of the relevant page:

- Remove all troubleshooting sections from every page. (5 minutes per page, 14 pages.)
- Remove all API reference tables from every page. (Varies, largest on Sagas, Routing, and Messages.)
- Remove consumer adapter documentation from Handlers. (One section removal.)
- Add EIP links where specified. (One line per link, major credibility improvement.)
- Add the "Next steps" bridge sentence at the end of every page, following the narrative arc specified in Part 1.
