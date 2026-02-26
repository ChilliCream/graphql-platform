Now I have comprehensive research. Let me compile the full structured report.

---

## Research Report: Mocha Transport Documentation (All Three Pages)

### Current Page Summary

**`transports/index.md` (Transports Overview)**

The overview page is well-structured and covers the essentials: a one-paragraph conceptual definition of what a transport is, a comparison table of the two available transports, a code-first "Add a transport" section showing how simple swapping is, a lifecycle diagram (Mermaid flowchart), a decision matrix for choosing between transports, and sections on transport-level configuration, middleware scoping, and handler binding modes. It closes with a troubleshooting section and next-step links.

What the page does well: The lifecycle phase table is unusually detailed and genuinely useful. The middleware scoping model (bus -> transport -> endpoint) is clearly explained. The troubleshooting section addresses real errors with causes and fixes.

What is missing or underdeveloped: The transport abstraction concept is mentioned in one sentence but not explained conceptually -- why this pattern exists, what benefit portability gives, and what the cost of switching is in practice. The decision matrix is functional but uses the same column headers as every other table in the docs, which reduces scanability.

---

**`transports/in-memory.md` (InMemory Transport)**

Follows a how-to structure: install, register, verify, then configuration options, custom topology, explicit endpoints, reference tables, and troubleshooting. The testing section is present but is a minimal two-step code block that does not discuss timing, concurrency, or isolation.

What the page does well: The reference section is comprehensive -- every method is tabulated with its default value and description. The "When to use" table at the bottom is clear and gives a direct yes/no signal.

What is missing: No explanation of the underlying model (topics, queues, bindings within a process -- what this looks like at runtime). The integration testing section does not mention waiting for handler completion (async timing issues), which is a common pain point. No discussion of test isolation between tests (shared state, resetting between test runs). No mention of concurrency behavior (does InMemory process messages in-order? parallel?).

---

**`transports/rabbitmq.md` (RabbitMQ Transport)**

Covers setup (Aspire path and manual path), custom topology declaration, receive endpoint configuration, prefetch and concurrency tuning, a topology explanation section with Mermaid diagram, auto-provisioned resource naming table, reference tables for all descriptors, and troubleshooting.

What the page does well: The prefetch/concurrency section gives concrete guidance (start with MaxPrefetch >= MaxConcurrency). The troubleshooting section covers the most common real-world errors. The auto-provisioned resource naming table is exceptionally useful. The Mermaid diagram of publisher -> exchange -> queue -> consumer is the clearest visual in the three pages.

What is missing: No explanation of why RabbitMQ uses two connections (one for publishing, one for consuming) -- this is mentioned as a fact in the lifecycle section of index.md but not explained in terms of back-pressure isolation. No explanation of why quorum queues are the recommended production queue type (the Queue descriptor shows the `x-queue-type` argument but does not say "use quorum for production"). No guidance on connection recovery or what happens when the broker drops mid-flight. No mention of the RabbitMQ management UI as a debugging tool for topology inspection (it's mentioned in "Verify it works" but not in Troubleshooting where it would help most). Publisher confirms are not mentioned.

---

### Competitor Analysis

#### NServiceBus (Particular Software)

**Documentation structure:** NServiceBus separates transport documentation into three distinct layers: (1) an overview page explaining the abstraction concept, (2) a dedicated transport selection guide with a decision flowchart and per-transport advantages/disadvantages, and (3) per-transport deep-dives with their own sub-pages for routing topology, connection settings, delayed delivery, transactions, native integration, and operations scripting.

**Transport abstraction explanation:** NServiceBus explicitly defines the abstraction concept in the overview: transports are implementations of an abstraction that allows teams to "spend more time delivering business features." The selection guide is a separate page that acknowledges decision difficulty and provides a flowchart-based decision process, per-transport trade-off analysis, and contextual use-case recommendations.

**RabbitMQ-specific depth:** The RabbitMQ transport has a dedicated routing topology page explaining how endpoint names map to exchanges and queues, how sending and publishing differ at the infrastructure level, and the distinction between fanout (conventional) and topic (direct) topologies. This is a level of conceptual depth that the Mocha RabbitMQ page does not reach. NServiceBus explicitly lists disadvantages of RabbitMQ (does not handle network partitions well), which is a transparency practice Mocha lacks entirely.

**In-memory/learning transport:** NServiceBus calls it the "Learning Transport" and explicitly warns it is NOT for production or even realistic development. It stores messages on disk as files, has a 64 KB message size limit, and runs at concurrency of 1. This is a completely different philosophical position from Mocha: NServiceBus says "use your real transport in development." A third-party `NServiceBus.Transport.InMemory` package exists for actual in-memory testing, but it is community-maintained, not official.

**What NServiceBus does well:** Dedicated transport selection guide with trade-off transparency. Per-transport sub-page structure allowing deep RabbitMQ coverage without cluttering the overview. Explicit "not for production" warnings with reasoning. Operations scripting documentation.

**What NServiceBus does poorly:** The RabbitMQ routing topology page provides no diagrams. The main transport page is dense and requires navigating many sub-pages to get a complete picture. Prefetch and concurrency tuning are not covered in the main docs.

---

#### MassTransit

**Documentation structure:** MassTransit has a short in-memory transport page and a longer RabbitMQ configuration page. The testing documentation is a separate, prominent section that explains the Test Harness as the primary testing primitive -- notably distinct from simply using the in-memory transport.

**In-memory transport framing:** MassTransit's in-memory transport page is brief and frames the transport as a tool that "replicates RabbitMQ's broker topology" (exchanges, queues, bindings) within a process. Critically, MassTransit separates the in-memory transport from the Test Harness: the Test Harness is the recommended testing path, and it provides observable message collections (published, sent, consumed) as well as a built-in scheduler. Simply using the in-memory transport directly for tests is not MassTransit's primary recommendation.

**RabbitMQ depth:** MassTransit's configuration page is thorough on topology: it explains the two-tier exchange structure (message-type exchange -> endpoint exchange -> queue), shows how publishing to unbound exchanges loses messages (important warning Mocha lacks), covers quorum queues, prefetch counts, and cloud provider-specific examples (CloudAMQP, AmazonMQ). They explain the `exchange:` vs `queue:` address format distinction. The lack of a troubleshooting section is a notable gap.

**What MassTransit does well:** Separating the Test Harness concept from the in-memory transport concept. Making the "messages lost if listener not started" warning prominent. CloudAMQP/AmazonMQ examples for hosted broker scenarios. Quorum queue guidance integrated into endpoint configuration.

**What MassTransit does poorly:** No troubleshooting section. Limited newcomer guidance on choosing address formats. The in-memory transport page is very short with minimal configuration guidance. The test harness is its own documentation island and requires the reader to connect the dots between transports and testing.

---

#### Wolverine (JasperFx)

**Documentation structure:** Wolverine's RabbitMQ transport documentation is a hierarchical multi-page structure with dedicated pages for Publishing, Listening, Dead Letter Queues, Conventional Routing, Queue/Topic/Binding Management, Topics, Interoperability, Multiple Brokers, and Multi-Tenancy. This is the most granular transport doc structure of the three competitors.

**Transport abstraction concept:** Wolverine does not prominently explain the transport abstraction concept on the RabbitMQ landing page. The documentation jumps directly into configuration. The testing documentation (separate section) is notable for its `StubAllExternalTransports()` API -- an explicit method that converts all external broker configurations into no-ops for test scenarios, while keeping all handler logic active. This is an elegant pattern Mocha lacks.

**In-memory transport framing:** Wolverine calls it "local" queues and treats it as a lightweight in-process queue that can be used like a transport. Testing uses `StubAllExternalTransports()` rather than switching transport types entirely. The testing philosophy is different: keep your production transport registered, just stub it out. The `InvokeMessageAndWaitAsync()` method waits for full message-chain completion, addressing the timing problem that integration tests with async handlers face.

**RabbitMQ depth:** Wolverine documents two-connection architecture explicitly ("one for listening, one for sending") with a `UseListenerConnectionOnly()` / `UseSenderConnectionOnly()` for resource optimization. Aspire integration has a specific note about URI vs connection string format difference. Multi-tenancy and multi-broker scenarios get their own documentation pages, which shows architectural scope beyond what Mocha currently documents.

**What Wolverine does well:** The `StubAllExternalTransports()` testing API is the cleanest integration-testing pattern of any competitor. The tracked session / `InvokeMessageAndWaitAsync()` approach eliminates timing issues in async tests. Dedicated sub-pages for every RabbitMQ concern. Explicit two-connection architecture explanation. Testing-first philosophy that does not require transport switching.

**What Wolverine does poorly:** No transport overview page with a comparison decision matrix. The documentation assumes RabbitMQ knowledge (exchange types, bindings) rather than explaining the concepts. No troubleshooting section. The multi-page structure makes it hard to get a complete picture quickly.

---

### Best Practices Found

From the research across competitors and authoritative sources:

1. **Quorum queues should be the explicit production recommendation.** RabbitMQ's own documentation and the CloudAMQP best practices guide both state quorum queues should be the default for production workloads requiring data safety. Classic mirrored queues were removed in RabbitMQ 4.0. Mocha's RabbitMQ page mentions `x-queue-type` in the queue descriptor table but does not say which type to use for production. This is a significant gap.

2. **Two-connection architecture needs explanation.** Both Wolverine and the RabbitMQ best practices guide explain why separate connections for publishing and consuming are important: a slow consumer does not block publishers via back-pressure, and vice versa. Mocha mentions this in the lifecycle section of index.md ("two connections: one for consuming, one for dispatching") but does not explain the reason.

3. **Prefetch and concurrency guidance from first principles.** The CloudAMQP guide explains prefetch from the formula: total round-trip time divided by per-message processing time. For slow handlers, prefetch should be 1 for fair distribution. For fast handlers, higher prefetch improves throughput. Mocha's guidance ("set MaxPrefetch equal to or slightly higher than MaxConcurrency") is a practical rule of thumb but does not explain the underlying model.

4. **Publisher confirms should be mentioned.** RabbitMQ's reliability guide identifies publisher confirms as essential for "at least once" delivery guarantees. Mocha's RabbitMQ page does not mention whether the transport uses publisher confirms or what delivery guarantee users get. MassTransit's host configuration exposes this setting.

5. **Testing with external transports vs replacing them.** Wolverine's pattern of `StubAllExternalTransports()` is architecturally cleaner than the pattern of "use InMemory transport in tests." It means integration tests run against the same configuration as production (same endpoints, same routing), just with the broker connection stubbed. Mocha recommends switching to InMemory for testing, which means test configuration diverges from production configuration.

6. **Explicit "not for production" warnings are necessary.** NServiceBus's Learning Transport documentation is emphatic: it is not for production, and even longer-term development should use the real transport. Mocha's InMemory page has a "When to use" table with "Multi-service production -- No" but does not frame this as urgently.

7. **Message loss scenarios need explicit callout.** MassTransit explicitly warns that if a publisher publishes to an exchange with no bound queue (subscriber not started), the message is lost. Mocha's documentation does not address this scenario, which is a real operational risk.

---

### External References

**Enterprise Integration Patterns (authoritative foundational references):**

- [Message Channel -- Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageChannel.html): The foundational pattern for understanding why transports exist as a concept. Mocha's transport overview could link to this to give the abstraction conceptual grounding.
- [Message Broker -- Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageBroker.html): Explains the hub-and-spoke model, decoupling senders from receivers, and scalability trade-offs of the broker pattern.
- [Message Bus -- Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageBus.html): Explains the common data model + common command set + messaging infrastructure combination -- directly relevant to what Mocha provides.

**RabbitMQ Official Documentation:**

- [Quorum Queues -- RabbitMQ](https://www.rabbitmq.com/docs/quorum-queues): Definitive reference for when and why to use quorum queues. Key production recommendation: minimum 3-node cluster, always durable, manual ack required, higher prefetch benefits quorum queue throughput.
- [Reliability Guide -- RabbitMQ](https://www.rabbitmq.com/docs/reliability): Covers publisher confirms, consumer acknowledgements, connection recovery, and idempotency. Essential context for what Mocha's acknowledgement middleware does internally.
- [Queues -- RabbitMQ](https://www.rabbitmq.com/docs/queues): Reference for queue types (classic, quorum, stream) and their characteristics.

**RabbitMQ Best Practices:**

- [Part 1: RabbitMQ Best Practices -- CloudAMQP](https://www.cloudamqp.com/blog/part1-rabbitmq-best-practice.html): Practical guidance on connection/channel management, prefetch tuning formula, queue configuration, and exchange selection. Authoritative source for production operational guidance.

**Competitor Documentation:**

- [Transports -- NServiceBus](https://docs.particular.net/transports/): Reference for how a mature ecosystem structures transport documentation.
- [Routing Topology -- NServiceBus RabbitMQ](https://docs.particular.net/transports/rabbitmq/routing-topology): Example of how to explain the mapping from framework concepts to RabbitMQ primitives.
- [Selecting a Transport -- NServiceBus](https://docs.particular.net/transports/selecting): Reference for how to write a transport selection guide with explicit trade-offs.
- [RabbitMQ Transport -- MassTransit](https://masstransit.io/documentation/transports/rabbitmq): Reference for quorum queue guidance integrated into endpoint configuration.
- [RabbitMQ Configuration -- MassTransit](https://masstransit.io/documentation/configuration/transports/rabbitmq): Reference for how to explain two-tier exchange topology and the message-loss warning.
- [Testing -- MassTransit](https://masstransit.io/documentation/concepts/testing): Reference for separating the test harness concept from the in-memory transport concept.
- [Test Automation Support -- Wolverine](https://wolverinefx.net/guide/testing.html): Reference for the `StubAllExternalTransports()` pattern and tracked session / wait-for-completion testing approach.
- [Using RabbitMQ -- Wolverine](https://wolverinefx.net/guide/messaging/transports/rabbitmq/): Reference for per-sub-topic RabbitMQ documentation structure and two-connection architecture explanation.

---

### Recommendations by Page

#### `transports/index.md` -- Transports Overview

**Recommendation 1: Add a brief conceptual paragraph on why transport abstraction matters.** One paragraph before the table explaining the portability benefit: same handlers, same patterns, same code -- different infrastructure. Reference the Enterprise Integration Patterns Message Broker concept. NServiceBus does this and it gives the abstraction legitimate weight rather than appearing to be marketing.

**Recommendation 2: Add a dedicated transport selection section with trade-off transparency.** The current decision matrix shows features but not trade-offs. NServiceBus's selection guide lists explicit disadvantages. Consider adding a "Trade-offs" subsection: InMemory loses messages on restart and cannot scale across processes; RabbitMQ requires operational expertise, has network partition limitations, and adds latency. Honest trade-offs build trust.

**Recommendation 3: Explain the two-connection model in the lifecycle section.** The lifecycle table mentions "two connections (one for consuming, one for dispatching)" but does not say why. One sentence: "Using separate connections prevents a slow consumer from applying back-pressure that blocks outbound dispatching."

**Recommendation 4: Add a multi-transport section.** The overview mentions `.Name("primary")` and multi-transport configurations but never explains what that scenario looks like or when you would need it. A brief paragraph or minimal example showing two transports registered simultaneously would complete the picture.

---

#### `transports/in-memory.md` -- InMemory Transport

**Recommendation 1: Restructure the testing section to address timing and isolation.** The current two-step test example does not address how to wait for async handler completion (the most common integration test problem). Add guidance analogous to Wolverine's `InvokeMessageAndWaitAsync()` -- does Mocha have a similar mechanism? If so, document it. If not, document what pattern users should follow (polling, delays, completion signals).

**Recommendation 2: Explain test isolation.** Two consecutive tests that publish to the same handler may interfere if the bus is shared. Document whether a new host instance per test is required, whether the bus can be reset, and whether handlers are cleared between test runs.

**Recommendation 3: Clarify the "single-process only" constraint more prominently.** Both MassTransit and NServiceBus make this a headline warning. Mocha's page buries it in the "When to use" table. Consider promoting it to the opening paragraph or a callout box.

**Recommendation 4: Add an explanation of what the in-process topology looks like.** The page mentions "topics, queues, and bindings" but does not describe what this means within a single process. A one-paragraph explanation (or minimal diagram) showing how an in-process message flows from publisher through a topic and queue to a handler would demystify the model and explain why InMemory can be swapped for RabbitMQ without code changes.

**Recommendation 5: Reinforce that InMemory is not a substitute for testing against a real broker.** NServiceBus explicitly says "use your production transport for development." Consider adding a callout: InMemory tests exercise handler logic but not RabbitMQ-specific behavior (topology conflicts, acknowledgement semantics, connection recovery). For testing broker-specific behavior, use a test broker.

---

#### `transports/rabbitmq.md` -- RabbitMQ Transport

**Recommendation 1: Add an explicit quorum queue recommendation.** The queue descriptor table shows `x-queue-type` as an argument but does not recommend which type. Add a subsection under "How to declare custom topology" or under the Queue descriptor table: "For production workloads, use quorum queues (`WithArgument("x-queue-type", "quorum")`). Quorum queues replicate across nodes using the Raft consensus algorithm. Classic queues were deprecated in RabbitMQ 4.0." Link to the RabbitMQ quorum queues documentation.

**Recommendation 2: Explain the two-connection model and its benefit.** Wolverine and MassTransit both address this. Add a brief subsection after "Set up the RabbitMQ transport": "Mocha opens two connections to the broker -- one for consuming and one for dispatching. This prevents back-pressure from slow consumers from blocking outbound message publishing."

**Recommendation 3: Add publisher confirms and delivery guarantee documentation.** Users need to know what delivery guarantee Mocha provides. Does the acknowledgement middleware use publisher confirms on dispatch? Is delivery "at least once" or "at most once"? This is the most operationally significant missing information on the page.

**Recommendation 4: Add a message-loss warning for unbound exchanges.** MassTransit documents this prominently: if a message is published to an exchange with no bound queue (no active consumer), the message is dropped. Mocha auto-provisions topology, but there is still a window during startup. Add: "Messages published before the transport completes its Start phase may be lost if no queue is bound to the exchange yet."

**Recommendation 5: Expand the troubleshooting section with management UI guidance.** The management UI (`http://localhost:15672`) is mentioned in "Verify it works" but not in Troubleshooting, where it is most useful. Add it as a tool for diagnosing: checking consumers attached to a queue, inspecting dead-letter queues, viewing message rates, and manually re-queuing dead-lettered messages.

**Recommendation 6: Add a "Manual connection string" example alongside the Aspire example.** The current setup section shows Aspire as the primary path and `IRabbitMQConnectionProvider` as the alternative. Add a third pattern: direct connection string configuration without Aspire and without a custom provider, for teams not using Aspire.

**Recommendation 7: Add an explicit note about quorum queue prefetch behavior.** The RabbitMQ quorum queues documentation notes that higher prefetch values benefit quorum queues because consumers should not be starved while acknowledgements flow through the consensus mechanism. This directly connects the prefetch/concurrency guidance to the queue type recommendation.
