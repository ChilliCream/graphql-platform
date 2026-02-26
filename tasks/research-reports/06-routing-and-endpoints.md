Now I have comprehensive research to produce the analysis. Let me compile the findings into a structured report.

---

## Research Report: Routing and Endpoints Documentation for Mocha

### Current Page Summary

**File:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/routing-and-endpoints.md`

The current page is comprehensive and well-structured. It covers:

1. **Introduction with a concrete quick-start block** -- shows a code registration and immediately explains all six endpoints that get created from it. This is a strong pedagogical choice.
2. **Verify endpoint discovery** -- offers the `Describe()` method as a debugging tool, with expected output shown. This is valuable and relatively rare in competitor docs.
3. **How routing works** -- explains inbound vs outbound routes with tables mapping handler interfaces to route kinds and endpoint types.
4. **Naming conventions** -- covers service name configuration, receive endpoint naming by route kind (subscribe vs send/request), publish endpoint naming with namespace prefix, and special endpoint names (error, skipped, reply).
5. **Endpoint types** -- explains receive vs dispatch endpoints with full property reference tables.
6. **The endpoint router** -- brief explanation of `IEndpointRouter`.
7. **Customize outbound routes** -- `AddMessage<T>()` with destination helpers (`ToQueue`, `ToExchange`, `ToTopic`).
8. **Bind consumers to endpoints** -- implicit vs explicit binding with code examples.
9. **Configure receive/dispatch endpoints** -- reference tables for `IReceiveEndpointDescriptor` and `IDispatchEndpointDescriptor`.
10. **Feature and scope precedence** -- three-level hierarchy (bus > transport > endpoint) with code and explanation of middleware scoping.
11. **Convention reference** -- `IBusNamingConventions` method table, convention type table, and full configuration property tables for `ReceiveEndpointConfiguration` and `DispatchEndpointConfiguration`.
12. **How the topology is built** -- six-phase initialization walkthrough.
13. **Troubleshooting** -- five scenarios with causes and fixes.
14. **Next steps** -- links to middleware, reliability, and transport pages.

**Strengths of the current page:**

- Exemplary "verify discovery" section with real output is uncommon and very useful.
- Concrete six-endpoint output from a minimal registration is a great teaching tool.
- Scope precedence (bus > transport > endpoint) is clearly explained with a working example.
- Troubleshooting section addresses realistic failure modes.
- Convention reference tables are thorough.

**Weaknesses of the current page (identified through research):**

- No conceptual framing before the code block. The page jumps immediately into code without establishing what an "endpoint" fundamentally is in messaging systems.
- The difference between logical and physical endpoints is never explicitly named. NServiceBus makes this distinction a central pillar of its routing docs.
- No visual diagram showing the flow: handler registration -> inbound route -> receive endpoint -> queue, and message type -> outbound route -> dispatch endpoint -> exchange/queue. Wolverine uses architecture diagrams in their introduction.
- The `IEndpointRouter` section is very brief (four sentences). The mental model of "how does `SendAsync` actually find where to send the message at runtime?" is underexplained.
- The EIP patterns (Message Endpoint, Point-to-Point Channel, Publish-Subscribe Channel) that underpin the design choices are never referenced. This leaves readers without vocabulary to relate Mocha concepts to the broader field.
- The six-phase initialization section ("How the topology is built") describes what happens but does not explain the implications: e.g., that topology errors surface at startup, not at send time.
- The naming convention tables lack the "why" -- why does the service prefix exist for subscribe routes but not send routes? It is documented but not explained at the conceptual level.

---

### Competitor Analysis

#### NServiceBus (Particular Software)

**Documentation URLs:**

- [Message routing](https://docs.particular.net/nservicebus/messaging/routing)
- [Endpoints and endpoint instances](https://docs.particular.net/nservicebus/endpoints/)
- [Specify endpoint name](https://docs.particular.net/nservicebus/endpoints/specify-endpoint-name)
- [Routing extensibility](https://docs.particular.net/nservicebus/messaging/routing-extensibility)
- [Step-by-step: Multiple endpoints tutorial](https://docs.particular.net/tutorials/nservicebus-step-by-step/3-multiple-endpoints/)

**How they explain endpoints:**
NServiceBus opens with a formal definition: an endpoint is "a logical component that communicates with other components using messages," with an endpoint instance being "a physical deployment of an endpoint." This logical/physical distinction is established immediately and carried through all subsequent routing docs. Multiple instances of the same logical endpoint constitute a single logical endpoint only when they share the same name, same handlers, and same environment -- this is explicit, which prevents common confusion about scaling.

**How they handle routing:**
They divide routing into three categories with named layers:

- **Logical routing** -- which logical endpoint should receive the message (commands: `RouteToEndpoint()`, events: native multicast or `RegisterPublisher()`)
- **Physical routing** -- which physical instance of the selected endpoint gets the message (instance-level discrimination via `MakeInstanceUniquelyAddressable()`)

Routes can be configured at assembly, namespace, or specific type granularity with explicit override precedence (type overrides namespace, namespace overrides assembly).

**What they do well:**

- The logical/physical routing distinction is conceptually powerful and prevents confusion.
- Tutorial-based learning ("Multiple Endpoints") demonstrates inter-process routing by having users break and fix it, which is highly effective.
- Extensibility docs expose the `UnicastRoutingTable` and `Publishers` collection with explicit thread-safety notes and conflict detection guidance.
- Clear error-driven pedagogy: showing what breaks when configuration is missing.

**What they do poorly:**

- Naming conventions for endpoints are almost entirely manual. There is no auto-discovery or convention-based endpoint naming equivalent to Mocha's `IBusNamingConventions`. Users must explicitly state `RouteToEndpoint(typeof(PlaceOrder), "Sales")` for every message type, which is verbose.
- No equivalent to Mocha's "verify endpoint discovery" (`Describe()` output). Debugging misconfigured routing requires running the application and observing failures.
- The troubleshooting section is thin compared to Mocha's.

#### MassTransit

**Documentation URLs:**

- [Configuration](https://masstransit.io/documentation/configuration)
- [Consumers](https://masstransit.io/documentation/configuration/consumers)
- [Topology](https://masstransit.io/documentation/configuration/topology)
- [Producers](https://masstransit.io/documentation/concepts/producers)
- [RabbitMQ Configuration](https://masstransit.io/documentation/configuration/transports/rabbitmq)

**How they explain endpoints:**
MassTransit's strongest conceptual contribution is making topology and routing explicitly separate concerns: "Topology cannot alter the destination of a message, only the properties of the message delivery itself. Determining the path of a message is routing, which is handled separately." This separation of concerns is well-communicated. Their topology documentation breaks into Bus Topology, Publish Topology, and Consume Topology.

For naming, `ConfigureEndpoints()` is the preferred auto-configuration approach, using an `IEndpointNameFormatter` to derive queue names from consumer class names in PascalCase. The default produces one queue per consumer, but `ConsumerDefinition` allows override with an explicit `EndpointName`.

**How they handle automatic routing:**
`ConfigureEndpoints(context)` is their convention-based approach -- it registers consumers, sagas, and routing slip activities automatically. Manually configured endpoints should be placed before this call ("Order Matters" is a named gotcha in their docs). Endpoint conventions (`EndpointConvention.Map<T>(uri)`) allow static mapping from message type to address, eliminating the need for repeated `GetSendEndpoint()` calls.

**What they do well:**

- The topology/routing separation is clearly stated and conceptually important. Mocha's docs blend topology configuration and routing configuration without naming this distinction.
- Short address notation (`queue:input-queue`) is well-explained as a simplified alternative to fully-qualified URIs.
- Scope rules for obtaining send endpoints ("from the closest scope") is documented with a clear priority list and explains why: to preserve correlation headers.
- RabbitMQ-specific exchange/queue topology is explained with concrete naming examples.

**What they do poorly:**

- The "why" behind one-queue-per-consumer default is never explained.
- Real-world guidance for large multi-consumer systems is sparse.
- The topology documentation has no code examples beyond interface descriptions; it is conceptual-only.
- No equivalent to Mocha's six-phase initialization walkthrough.

#### Wolverine (JasperFx)

**Documentation URLs:**

- [Message Routing](https://wolverinefx.net/guide/messaging/subscriptions.html)
- [Getting Started with Wolverine as Message Bus](https://wolverinefx.net/guide/messaging/introduction.html)

**How they explain routing:**
Wolverine's routing documentation presents a five-rule priority hierarchy for how subscriptions are resolved when a message is published:

1. Message forwarding (type transformation)
2. Explicit routing rules
3. Local queue conventions
4. External broker conventions
5. Custom routing conventions

This ordered-priority model is excellent for debugging "why is my message going here?" questions. Their diagnostic tooling is the standout feature: they provide both programmatic access (`MessageBus.PreviewSubscriptions(type)`) and CLI commands to preview routing decisions before running.

**What they do well:**

- The five-rule priority hierarchy makes routing predictable and debuggable.
- CLI diagnostic commands for previewing routing are excellent developer ergonomics.
- Progressive disclosure: separates "Listening Endpoint Configuration" and "Sending Endpoint Configuration" as distinct topics.
- Support for conventional and explicit routing is shown side-by-side.
- `IMessageRoutingConvention` for custom conventions is shown with a concrete example (routing by message type name prefix to different RabbitMQ exchanges).

**What they do poorly:**

- The introduction page has empty "Listening Endpoint Configuration" and "Sending Endpoint Configuration" sections -- they are placeholder headings with no content, which is a significant gap.
- The `IMessageRoute` interface documentation lacks context for developers building custom patterns.
- No equivalent to Mocha's naming convention tables or property reference tables.
- Critical features (like additive local routing added in v3.6) are buried late in pages without prominence.

---

### Best Practices Found Across All Sources

**1. Name the logical/physical distinction explicitly.** NServiceBus's clearest contribution is naming this: a logical endpoint is the concept, a physical endpoint instance is the deployment. Mocha conflates these implicitly. Naming the distinction helps users reason about scaling, competing consumers, and service naming.

**2. Show routing decisions in a priority order.** Wolverine's five-rule hierarchy is the best approach for routing documentation. When multiple factors could influence where a message goes (explicit routes, conventions, service name), presenting them as a ranked priority list gives users a mental model for predicting and debugging behavior.

**3. Provide diagnostic tooling and document it prominently.** Wolverine's CLI routing preview and Mocha's `Describe()` method serve the same purpose. Mocha already does this well but could expand it -- e.g., showing how to check the current routing table for a specific message type at startup.

**4. Separate topology from routing conceptually.** MassTransit's explicit statement that "topology determines delivery properties, routing determines destination" prevents confusion. Mocha's "How the topology is built" section and routing sections exist but are not framed against each other.

**5. Explain the "why" behind naming conventions.** Why does a subscribe endpoint get a service prefix but a send endpoint does not? The answer (fan-out vs point-to-point delivery semantics) maps directly to EIP's Point-to-Point Channel vs Publish-Subscribe Channel distinction. Naming this connection would make the conventions feel inevitable rather than arbitrary.

**6. Use error-driven pedagogy.** NServiceBus's tutorial shows what breaks when routing is missing. Mocha's troubleshooting section partially does this, but extending the troubleshooting scenarios to include "what you'll see in logs" would add practical value.

**7. Include a visual topology diagram.** No competitor fully nails this, but Wolverine uses architecture diagrams at the introduction level. A diagram showing handler -> inbound route -> receive endpoint -> queue/exchange, and message type -> outbound route -> dispatch endpoint -> exchange/queue would accelerate understanding.

---

### External References

**Enterprise Integration Patterns (foundational):**

- [Message Endpoint](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageEndpoint.html) -- Defines the concept of an endpoint as a bridge between application code and messaging infrastructure. Directly underpins Mocha's receive/dispatch endpoint distinction.
- [Message Router](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageRouter.html) -- The pattern underlying Mocha's routing conventions. Establishes that a router "does not modify message contents, only the destination."
- [Point-to-Point Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PointToPointChannel.html) -- The pattern underlying send/request routes (dedicated queues with exclusive consumer semantics).
- [Message Routing Introduction](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageRoutingIntro.html) -- Organizes routing patterns into: Simple Routers (Content-Based Router, Dynamic Router, Recipient List), Composed Routers (Scatter-Gather, Routing Slip), and Architectural Patterns (Message Broker). The Content-Based Router pattern is what Mocha implements when resolving outbound routes by message type.
- [EIP Chapter 3 PDF](https://www.enterpriseintegrationpatterns.com/docs/EnterpriseIntegrationPatterns_HohpeWoolf_ch03.pdf) -- Full messaging systems chapter.

**Authoritative practitioner sources:**

- [Message Naming Conventions by Jimmy Bogard](https://www.jimmybogard.com/message-naming-conventions/) -- The MassTransit author on command vs event naming patterns. Directly relevant to Mocha's suffix-stripping convention (`Command`, `Event`, `Message`, `Query`, `Response` suffixes). Bogard notes that the `Command`/`Event` suffix aids automatic identification but can be redundant -- important context for why Mocha strips these suffixes when deriving endpoint names.
- [Patterns of Distributed Systems (Martin Fowler)](https://martinfowler.com/articles/patterns-of-distributed-systems/) -- Catalog of patterns referenced for distributed systems context.

**Competitor primary sources:**

- [NServiceBus Message Routing](https://docs.particular.net/nservicebus/messaging/routing) -- Logical/physical routing distinction, command vs event vs reply routing categories.
- [NServiceBus Endpoints and Endpoint Instances](https://docs.particular.net/nservicebus/endpoints/) -- Formal endpoint definition, logical vs physical instance explanation.
- [NServiceBus Routing Extensibility](https://docs.particular.net/nservicebus/messaging/routing-extensibility) -- `UnicastRoutingTable`, runtime route updates via `FeatureStartupTask`, thread-safe route management.
- [MassTransit Topology](https://masstransit.io/documentation/configuration/topology) -- Topology vs routing separation, bus/publish/consume topology layers.
- [MassTransit Consumers](https://masstransit.io/documentation/configuration/consumers) -- `ConfigureEndpoints()`, `ConsumerDefinition`, naming formatter.
- [MassTransit Producers](https://masstransit.io/documentation/concepts/producers) -- Send vs Publish, short address notation, scope rules for endpoint resolution.
- [MassTransit RabbitMQ Configuration](https://masstransit.io/documentation/configuration/transports/rabbitmq) -- Transport-specific topology, exchange/queue naming.
- [Wolverine Message Routing](https://wolverinefx.net/guide/messaging/subscriptions.html) -- Five-rule priority hierarchy, diagnostic tooling, `IMessageRoutingConvention`.
- [Wolverine Getting Started](https://wolverinefx.net/guide/messaging/introduction.html) -- Transport configuration patterns, multi-transport organization.

---

### Recommendations: Specific Improvements

**Priority 1 -- High Impact, Low Effort:**

1. **Add a two-sentence conceptual opening paragraph** before the code block that frames what an endpoint is in messaging systems terms. Something like: "An endpoint is the combination of a transport address (a queue or exchange) and a pipeline of code that processes messages. Mocha distinguishes between receive endpoints (which consume) and dispatch endpoints (which produce). Every message you handle or send has a corresponding endpoint; the conventions exist to derive endpoint names automatically from your handler and message types." This grounds readers who haven't read the quick-start in isolation.

2. **Add a "Why the service prefix?" callout** in the Naming Conventions section. The current page states that subscribe endpoints include the service prefix but does not explain why. The answer is the Point-to-Point Channel vs Publish-Subscribe Channel distinction: events are published to an exchange/topic (fan-out), and each subscribing service needs its own queue to receive its own copy. Without a service-specific prefix, two services would compete on the same queue, each consuming only half the events. Naming this reason (and linking to EIP's [Point-to-Point Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PointToPointChannel.html)) makes the convention feel logical.

3. **Expand the "No route for message type" troubleshooting entry** to include what the actual error message looks like. Developers debugging this issue will be searching log output, not documentation headings.

**Priority 2 -- Medium Impact:**

4. **Name the logical/physical distinction** in the "How routing works" or "Endpoint types" section. Borrow from NServiceBus: a logical endpoint is the named concept (e.g., "order-service.order-placed"), while a physical endpoint instance is the running consumer bound to that queue. This distinction matters when multiple service instances run simultaneously (competing consumers) and when reasoning about `MaxConcurrency` (per-instance, not per-logical-endpoint).

5. **Add a "routing priority" or "how the dispatcher resolves a route" section.** Wolverine's five-rule hierarchy is the most useful thing in their routing docs. Mocha has implicit priority (explicit `AddMessage<T>()` overrides convention; convention overrides nothing), but this is never stated as a rule. A two-step rule would suffice: "(1) If an explicit outbound route is registered with `AddMessage<T>()`, use it. (2) Otherwise, derive the endpoint name from naming conventions."

6. **Add a "topology vs routing" note** to the "How the topology is built" section. MassTransit's clean statement -- "Topology cannot alter the destination of a message, only the properties of the message delivery itself" -- is worth adapting. In Mocha's terms: "Topology conventions provision broker resources (queues, exchanges, bindings). Route conventions determine which resource each message type targets. The two phases are separate: topology runs at startup, routing runs at send/publish time."

**Priority 3 -- Lower Impact but Valuable:**

7. **Add EIP references in footnotes or a "Further reading" section** for the Message Endpoint, Point-to-Point Channel, and Content-Based Router patterns. These give advanced users vocabulary to reason about Mocha's design decisions relative to the broader field.

8. **Show a small topology diagram.** Even a simple ASCII art or inline Mermaid diagram showing the data flow from handler registration through to a queue/exchange would accelerate comprehension for visual learners. No competitor does this well; Mocha could differentiate here.

9. **Add a note about the implications of startup-time topology.** The current "How the topology is built" section describes the six phases but doesn't note an important consequence: topology errors (e.g., a mis-configured exchange) surface at startup, not at the first `SendAsync` call. This is a significant operational advantage over frameworks where routing errors are discovered only at message-send time.
