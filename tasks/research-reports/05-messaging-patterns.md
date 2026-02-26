Now I have comprehensive research. Let me compile the full structured report.

---

## Research Report: Messaging Patterns Documentation

### Current Page Summary

The Mocha `messaging-patterns.md` page covers all three core patterns in a single, long-form page:

- A top-level comparison table mapping pattern to bus method, handler interface, and delivery semantics
- Three major sections: Events (pub/sub), Commands (send), and Request/Reply
- Each section follows a consistent structure: concept introduction, end-to-end example (define message -> implement handler -> register -> invoke -> expected output), one "how to" secondary example, and a full API reference for the bus method and its options
- A "when to use which pattern" decision table
- A troubleshooting section with four common problems

The page is code-heavy and practical. It does not link to Enterprise Integration Patterns or any external theory. It does not explain the architectural rationale for why the patterns exist or the trade-offs of each.

---

### Competitor Analysis

#### NServiceBus (Particular Software)

**Documentation structure**: NServiceBus splits messaging patterns across multiple pages. There is a dedicated conceptual page ("Messages, Events, and Commands") separate from the operational pages ("Publish and Handle an Event", "Replying to a Message", "Full Duplex" sample). This creates a clear separation between "what are these things and why" versus "how do I use them."

**How they explain publish vs send**: NServiceBus enforces the distinction at runtime. Commands cannot be published; events cannot be sent. The framework throws specific exceptions if you violate the semantic contract. This is a fundamentally different approach from Mocha: rather than documenting a convention, NServiceBus bakes it into the type system and runtime validation.

**Architectural concepts woven in**: Commands follow verb-noun naming ("UpdateCustomerAddress", "SubmitOrder"). Events follow noun-verb-past-tense ("CustomerAddressUpdated", "OrderShipped"). This naming convention is documented as architectural guidance, not just style preference. They state explicitly: "A command tells a service to do something and should only be consumed by a single consumer." The documentation emphasizes message ownership: events are published by their owner, commands are sent to their logical owner.

**Request/reply approach**: NServiceBus treats request/reply as an advanced pattern with a separate callbacks extension (`NServiceBus.Callbacks`). They explicitly warn this should only be used for integrating legacy synchronous APIs into a messaging system. The "Full Duplex" sample is a separate document. Reply messages are classified as neither commands nor events (they implement `IMessage`, not `ICommand` or `IEvent`).

**EIP links**: Yes. They reference the Wikipedia pub/sub article and conceptually trace back to EIP foundations. The architecture principles page references Gregor Hohpe's work.

**Page structure**: Multiple pages. One-pattern-per-page for operational content. One unified conceptual page for definitions.

**What they do WELL**:

- Runtime enforcement of semantic rules prevents architectural anti-patterns
- Past-tense event naming and verb-noun command naming is concrete, memorable guidance
- Clear ownership semantics: events have one publisher, commands have one handler
- Excellent separation of conceptual vs. operational documentation

**What they do POORLY**:

- Request/reply is deprioritized and treated as a legacy integration bridge, not a first-class pattern
- Multiple-page structure can make it hard to get an overview in one place
- The runtime enforcement adds ceremony for simple use cases

---

#### MassTransit

**Documentation structure**: MassTransit organizes by operation type: a "Producers" page covers both publish and send together. A separate "Requests" page covers request/reply with `IRequestClient`. A "Messages" concepts page covers the command/event distinction at the conceptual level. Three pages total for the same content Mocha covers in one.

**How they explain publish vs send**: MassTransit's explanation is the clearest of the three competitors. From their documentation: "When a message is sent, it is delivered to a specific endpoint using a DestinationAddress. When a message is published, it is not sent to a specific endpoint, but is instead broadcasted to any consumers which have subscribed to the message type." They explicitly map: sent messages = commands; published messages = events. They also note transport-specific cost implications: on Azure Service Bus and Amazon SQS, publishing commands incurs extra topic-to-queue forwarding charges, so high-volume scenarios should `Send` commands directly.

**Architectural concepts woven in**: MassTransit provides naming convention guidance (verb-noun for commands, noun-verb-past-tense for events) identical to NServiceBus. They document one-to-one consumer constraints for commands. The EIP Publish-Subscribe Channel pattern is referenced directly. The "Messages" page explicitly links to the EIP catalog.

**Request/reply approach**: Well-documented via `IRequestClient<TRequest>`. They emphasize async-first design with `await`. They show how to handle multiple response types (positive and negative outcomes) without relying on exception flow. They also document timeout configuration and what happens when a consumer throws (a `RequestFaultException` is thrown on the requestor side). The `GetResponse<T1, T2>()` overload for multiple response types is a distinctive feature.

**EIP links**: Yes. MassTransit directly references the "Publish-Subscribe Channel" EIP pattern on their Producers page.

**What they do WELL**:

- Clearest distinction between send and publish with concrete rationale
- Multiple response type support for request/reply is architecturally sophisticated
- Transport cost implications for publish vs. send is practical production guidance missing from all others
- EIP references anchor the patterns in theory

**What they do POORLY**:

- Three pages of concepts before you see a working example
- `IRequestClient` setup requires more ceremony (container registration) than other frameworks
- Limited guidance on when NOT to use request/reply

---

#### Wolverine (JasperFx)

**Documentation structure**: A single "Sending Messages with IMessageBus" page covers publish, send, and invoke together. Wolverine's conceptual model maps closely to what Mocha does.

**How they explain publish vs send**: Wolverine's distinction is the simplest of the three: `SendAsync()` asserts at least one subscriber exists and throws if none are found. `PublishAsync()` sends if there is a known subscriber and silently ignores if there is not. This behavioral difference is the only documented distinction—Wolverine does not use commands/events terminology explicitly in the sending API, though their documentation discusses event subscriptions separately.

**Request/reply approach**: Wolverine uses `InvokeAsync<T>()` for request/reply within the same process (like MediatR). For remote request/reply, they also support this but it is less prominent than the local invocation use case. They link directly to the EIP RequestReply page: `https://www.enterpriseintegrationpatterns.com/RequestReply.html`.

**No required interfaces**: Wolverine's biggest architectural differentiator. Handlers are plain classes with methods named `Handle()` or `Consume()` - no `IHandler<T>` interface required. This reduces ceremony but the documentation acknowledges "some developers will decry this as too much magic."

**EIP links**: Yes. Wolverine explicitly links to the EIP RequestReply pattern URL and references the Microsoft Architecture Center pub/sub pattern.

**What they do WELL**:

- Convention-over-configuration handler registration is genuinely simpler
- The `SendAsync` vs `PublishAsync` semantic difference (at-least-one vs fire-and-forget) is a clean mental model
- EIP links are embedded naturally in the documentation flow

**What they do POORLY**:

- Lack of explicit command/event terminology makes the conceptual model harder to learn
- Less guidance on the architectural implications of choosing each pattern
- Request/reply in distributed (cross-process) scenarios is underexplored

---

### Best Practices Found

**From NServiceBus**: Naming conventions carry semantic weight. Verb-noun commands signal intent to one owner. Noun-verb-past-tense events signal notification to many. This convention should be stated as a rule, not just a style suggestion.

**From MassTransit**: Transport cost implications of publish vs. send matter in production. For RabbitMQ the overhead is negligible; for Azure Service Bus and Amazon SQS, publishing commands to topics with queue forwarding adds latency and cost. This is real-world production guidance that distinguishes mature documentation.

**From MassTransit**: Multiple response types for request/reply is a better pattern than exception flow. Rather than `try/catch RequestFaultException`, the `GetResponse<TSuccess, TFailure>()` approach keeps the happy path and error path as first-class typed results.

**From Wolverine**: The `SendAsync` vs `PublishAsync` behavioral guarantee (throws if no subscriber vs. silently discards) maps cleanly to the command vs. event semantic. This is a good way to explain the delivery guarantee difference without using heavy architectural terminology.

**From EIP literature**: The Request-Reply pattern uses a Correlation Identifier and a Return Address (reply queue). The requestor specifies a reply address in the request message itself; the replier does not hardcode a destination. This is what Mocha's `ReplyEndpoint` option in `SendOptions` implements, but the current documentation does not explain the underlying EIP mechanism. Explaining this gives readers a mental model for what happens when `RequestAsync` blocks.

**From the event-driven.io / CodeOpinion research**: The most common anti-pattern is using events when you actually mean commands ("OrderCreated" that assumes inventory will reserve stock is a command disguised as an event). Documenting this specific anti-pattern with an example gives the patterns page real architectural weight.

**From the Azure Architecture Center**: When you need bi-directional communication in a pub/sub system, use the Request/Reply pattern. The Microsoft documentation explicitly cross-links these two patterns. This framing (request/reply as a solution to the limitation of pure pub/sub) is a useful architectural narrative.

---

### External References

**Enterprise Integration Patterns (EIP) - Gregor Hohpe & Bobby Woolf**

These are the canonical foundational references. All three competitors trace back to EIP concepts, and MassTransit and Wolverine link directly.

- Publish-Subscribe Channel: `https://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html`
  Covers: The canonical definition of pub/sub, how a single input channel is split into per-subscriber output channels, the "each subscriber gets their own copy" guarantee, and the eavesdrop/monitoring use case. The framing question is "How can the sender broadcast an event to all interested receivers?"

- Command Message: `https://www.enterpriseintegrationpatterns.com/patterns/messaging/CommandMessage.html`
  Covers: Commands as a way to invoke a procedure in another application using messaging rather than RPC. Notably, EIP states "a Command Message is simply a regular message that happens to contain a command" - the distinction is semantic intent, not type system enforcement.

- Event Message: `https://www.enterpriseintegrationpatterns.com/patterns/messaging/EventMessage.html`
  Covers: The critical insight that event content may be minimal ("their mere occurrence tells the observer to react") and that timing is more important than delivery guarantee. Event messages prioritize message expiration over guaranteed delivery, which is the architectural reason Mocha's `PublishOptions` has `ExpirationTime`.

- Request-Reply: `https://www.enterpriseintegrationpatterns.com/patterns/messaging/RequestReply.html`
  Covers: The two requestor implementations (synchronous blocking vs asynchronous callback), why replies are always point-to-point even when requests use pub/sub, and the Correlation Identifier and Return Address supporting patterns. This explains what Mocha's `ReplyEndpoint` option does mechanically.

- .NET Request/Reply Example: `https://www.enterpriseintegrationpatterns.com/patterns/messaging/RequestReplyNetExample.html`
  Covers: A concrete .NET MSMQ implementation showing Correlation IDs and Return Address in action. The replier reads the reply queue from the request message rather than hardcoding it. This is directly relevant to how Mocha implements request/reply.

**Martin Fowler**

- "What do you mean by Event-Driven?": `https://martinfowler.com/articles/201701-event-driven.html`
  Covers: Four distinct event-driven patterns (Event Notification, Event-Carried State Transfer, Event Sourcing, CQRS). The key insight for a messaging patterns page is the distinction between Event Notification (what pub/sub does) and the other patterns. Fowler's warning that "confusing these patterns causes serious problems" is authoritative justification for why the messaging-patterns page needs clear pattern definitions.

**Microsoft Azure Architecture Center**

- Publisher-Subscriber Pattern: `https://learn.microsoft.com/en-us/azure/architecture/patterns/publisher-subscriber`
  Covers: Comprehensive treatment of when to use pub/sub and when NOT to (near real-time interaction required, or very few consumers with very different needs). The "Issues and considerations" section covers idempotency, message ordering, poison messages, message expiration, and message scheduling - all directly relevant to Mocha's `PublishOptions`. The page cross-links to the EIP Request/Reply pattern for bi-directional scenarios.

**Architecture / Design blogs**

- CodeOpinion "Commands & Events: What's the difference?": `https://codeopinion.com/commands-events-whats-the-difference/`
  Covers: Commands are intent to invoke behavior (single owner-consumer). Events are facts that occurred (single publisher, zero-to-many subscribers). The flow commands-trigger-events is clearly explained. This is the most concise and linkable external explanation of the distinction.

- Ben Morris "Messaging Anti-Patterns in Event-Driven Architecture": `https://www.ben-morris.com/event-driven-architecture-and-message-design-anti-patterns-and-pitfalls/`
  Covers: Seven messaging anti-patterns including the "command in disguise" (publishing events that expect specific consumer behavior), entity-based events (CRUD events instead of business events), and assumed ordering. These anti-patterns are directly relevant to the troubleshooting section.

---

### Recommendations

**1. Add an architectural framing introduction before the table**

The current page opens with the table immediately. NServiceBus and MassTransit both place a conceptual paragraph before any code. The table is useful as a reference but is a poor first encounter with the patterns. A two-paragraph introduction explaining that each pattern exists to answer a different question ("who needs to know?", "who should act?", "what is the result?") would anchor the table in meaning.

**2. Link to EIP for each pattern's first mention**

MassTransit links to the EIP Publish-Subscribe Channel on their Producers page. Wolverine links to EIP RequestReply. Mocha currently has no external theory links at all. Adding a single sentence and link to the canonical EIP page for each pattern gives readers a foundation and signals the documentation is grounded in established theory. Example pattern: "This implements the [Publish-Subscribe Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html) pattern from Enterprise Integration Patterns."

**3. Add the command/event naming convention as explicit guidance**

Both NServiceBus and MassTransit document that commands use verb-noun present tense ("ReserveInventory", "ProcessRefund") and events use noun-verb past tense ("OrderPlaced", "PaymentCompleted"). Mocha's examples already follow this convention (the code uses `OrderPlacedEvent` and `ReserveInventoryCommand`) but it is never stated as a rule. Stating it explicitly gives readers a decision tool when naming new message types.

**4. Explain the "command in disguise" anti-pattern**

The current troubleshooting section covers operational failures (missing registrations, timeouts, duplicate delivery) but not the most common architectural mistake: using `PublishAsync` for a message that only one service should handle, or vice versa. A single "When to use which pattern" callout that addresses "I published an event but only one service should react" would prevent the single most common messaging architecture mistake.

**5. Add transport-cost awareness for publish vs. send (from MassTransit's playbook)**

MassTransit's documentation notes that publishing commands on Azure Service Bus and Amazon SQS incurs topic-to-queue forwarding overhead not present on RabbitMQ. When Mocha supports RabbitMQ and other transports, the analogous guidance should appear in the transport-specific documentation or cross-linked here.

**6. Explain the mechanical underpinning of request/reply**

The current page explains `RequestAsync` from the API surface down but does not explain what happens at the transport level. A single paragraph explaining Correlation ID and Reply Queue mechanics (that Mocha creates a temporary reply address, embeds it in the request envelope, and the handler sends the response back to that address) would demystify `ResponseTimeoutException` and `ReplyEndpoint` in `SendOptions`. Link: EIP Request-Reply pattern and the .NET example at `https://www.enterpriseintegrationpatterns.com/patterns/messaging/RequestReplyNetExample.html`.

**7. Consider a "see also" section linking to authoritative external resources**

The page could end with a curated set of external links rather than just the internal "Next steps" pointer:

- EIP Publish-Subscribe Channel
- EIP Command Message
- EIP Event Message
- EIP Request-Reply
- Martin Fowler "What do you mean by Event-Driven?"
- Microsoft Azure Architecture Center: Publisher-Subscriber pattern

This is low effort and immediately elevates the page's authority.

Sources:

- [Publish-Subscribe Channel - EIP](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html)
- [Command Message - EIP](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CommandMessage.html)
- [Event Message - EIP](https://www.enterpriseintegrationpatterns.com/patterns/messaging/EventMessage.html)
- [Request-Reply - EIP](https://www.enterpriseintegrationpatterns.com/patterns/messaging/RequestReply.html)
- [.NET Request/Reply Example - EIP](https://www.enterpriseintegrationpatterns.com/patterns/messaging/RequestReplyNetExample.html)
- [NServiceBus: Messages, Events, and Commands](https://docs.particular.net/nservicebus/messaging/messages-events-commands)
- [NServiceBus: Publish-Subscribe](https://docs.particular.net/nservicebus/messaging/publish-subscribe/)
- [NServiceBus: Publish and Handle an Event](https://docs.particular.net/nservicebus/messaging/publish-subscribe/publish-handle-event)
- [NServiceBus: Full Duplex Sample](https://docs.particular.net/samples/fullduplex/)
- [NServiceBus: Callbacks](https://docs.particular.net/nservicebus/messaging/callbacks)
- [MassTransit: Producers](https://masstransit.io/documentation/concepts/producers)
- [MassTransit: Messages](https://masstransit.io/documentation/concepts/messages)
- [MassTransit: Requests](https://masstransit.io/documentation/concepts/requests)
- [Wolverine: Sending Messages with IMessageBus](https://wolverine.netlify.app/guide/messaging/message-bus.html)
- [Wolverine: Message Handlers](https://wolverinefx.net/guide/handlers/)
- [Martin Fowler: What do you mean by Event-Driven?](https://martinfowler.com/articles/201701-event-driven.html)
- [Microsoft Azure Architecture Center: Publisher-Subscriber Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/publisher-subscriber)
- [CodeOpinion: Commands & Events - What's the difference?](https://codeopinion.com/commands-events-whats-the-difference/)
- [Ben Morris: Messaging Anti-Patterns in Event-Driven Architecture](https://www.ben-morris.com/event-driven-architecture-and-message-design-anti-patterns-and-pitfalls/)
- [MassTransit Send vs. Publish](https://www.maldworth.com/2015/10/27/masstransit-send-vs-publish/)
