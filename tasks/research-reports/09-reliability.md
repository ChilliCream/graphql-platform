Now I have sufficient research material to write a comprehensive structured report. Let me compile everything.

---

## Research Report: Mocha Reliability Documentation

### Current Page Summary

**File:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/reliability.md`

**Structure:**
The page is organized into eight sections with a troubleshooting tail:

1. Opening framing paragraph + quick-start configuration block
2. "The receive pipeline and failure flow" (ASCII pipeline diagram)
3. "Handle faults" (exception routing to error queue, fault headers table, verify snippet)
4. "Route unhandled messages to the dead-letter endpoint"
5. "Expire stale messages" (DeliverBy/ExpirationTime with publish and send examples)
6. "Limit concurrency" (SemaphoreSlim, config scoping, options reference table)
7. "Configure the circuit breaker" (Polly-backed, two CB scopes, options reference table)
8. "Guarantee delivery with the transactional outbox" (dual-write problem statement, Postgres setup walkthrough, outbox processor internals, skip-outbox escape hatch)
9. "Troubleshooting" (four Q&A entries)
10. "Next steps" (cross-links)

**Content characteristics:**

- Dual-write problem is named and explained in one paragraph before the outbox walkthrough.
- Outbox setup is stepwise with numbered actions and a call-purpose table.
- Options reference tables exist for `ConcurrencyLimiterOptions` and `CircuitBreakerOptions`.
- Two distinct circuit breaker scopes (transport-level vs. receive-level) are explained.
- Idempotency, inbox pattern, and message deduplication are entirely absent.
- No diagrams (ASCII pipeline diagram is the only visual aid).
- No comparison between at-least-once and exactly-once semantics.
- No external links to canonical pattern references.

---

### Competitor Analysis

#### NServiceBus (Particular Software)

**Outbox documentation:** [docs.particular.net/nservicebus/outbox/](https://docs.particular.net/nservicebus/outbox/)

NServiceBus does not call it the "dual-write problem." They reframe it as "the consistency problem" using a concrete `User` creation example and label failure modes as "zombie records" (data persisted, message lost) and "ghost messages" (message sent, data not persisted). This concrete vocabulary is highly effective for developers who have never encountered the pattern academically.

Their page structure is problem → visual flowchart (two phases) → implementation → important design considerations → persistence-specific links. The visual flowchart showing Phase 1 (database persistence) and Phase 2 (dispatch) is a standout feature that Mocha lacks entirely.

NServiceBus also covers the **transactional session** as a separate concept for when no incoming message context exists, which reflects a gap in Mocha's coverage.

**Recoverability documentation:** [docs.particular.net/nservicebus/recoverability/](https://docs.particular.net/nservicebus/recoverability/)

NServiceBus splits retry behavior into two named tiers — **Immediate Retries** (transient failures, no delay, up to 5 by default) and **Delayed Retries** (escalating delays: 10s, 20s, 30s, then error queue) — and presents **Automatic Rate Limiting** as the circuit-breaker equivalent. They also include concrete formulas showing how to calculate total retry attempts accounting for scale-out.

The concept of "recoverability" is treated as a distinct, named feature with its own documentation section. This gives it architectural weight. Mocha folds fault handling, dead-letter, and expiry into "reliability" without introducing the word "recoverability" or distinguishing retry tiers.

**What NServiceBus does well:** Named failure vocabularies (zombie/ghost), visual phase diagrams, splitting reliability into recoverability (retry) and outbox (durability) as separate concerns. They cross-reference the outbox from their consistency architecture page ([docs.particular.net/architecture/consistency](https://docs.particular.net/architecture/consistency)), which frames distributed consistency as a progressive challenge with outbox as one tool in a broader toolkit.

**What NServiceBus does poorly:** Outbox configuration code is sparse (a single line `endpointConfiguration.EnableOutbox()`). No troubleshooting. No guidance on what happens when outbox dispatch fails repeatedly.

---

#### MassTransit

**Outbox documentation:**

- Conceptual: [masstransit.io/documentation/patterns/transactional-outbox](https://masstransit.io/documentation/patterns/transactional-outbox)
- Configuration: [masstransit.io/documentation/configuration/middleware/outbox](https://masstransit.io/documentation/configuration/middleware/outbox)

MassTransit splits outbox documentation into two pages: a conceptual "patterns" page and a configuration page. The conceptual page explicitly distinguishes a **Bus Outbox** (captures publishes/sends before SaveChanges) from a **Consumer Outbox** (combines inbox and outbox for exactly-once consumer processing). This Bus vs. Consumer Outbox distinction is the most architecturally significant thing MassTransit explains that Mocha does not.

The configuration page is thorough, with options tables for `MessageDeliveryLimit`, `QueryDelay`, and `IsolationLevel`. It covers three supporting database tables (`InboxState`, `OutboxMessage`, `OutboxState`) and requires database lock provider configuration (PostgreSQL, SQL Server, MySQL). This level of configuration depth is notably greater than Mocha's current setup.

**Exceptions/retry documentation:** [masstransit.io/documentation/concepts/exceptions](https://masstransit.io/documentation/concepts/exceptions)

MassTransit has a standalone "Exceptions" page covering retry policies (Immediate, Interval, Incremental, Exponential), `Fault<T>` events as a publishable type that sagas and dashboards can consume, and redelivery for long-delay retries. Their "retry policies table" comparing all retry types side-by-side is a strong documentation pattern.

They also introduce an **In-Memory Outbox** ([masstransit.io/documentation/patterns/in-memory-outbox](https://masstransit.io/documentation/patterns/in-memory-outbox)) as a lighter-weight alternative for cases where persistence is not needed, which makes the trade-off visible.

**What MassTransit does well:** Explicit Bus vs. Consumer Outbox distinction, `Fault<T>` as a first-class publishable event (enabling operational dashboards), retry type comparison table, visual flow diagrams (4 diagrams on the transactional outbox page).

**What MassTransit does poorly:** The conceptual outbox page has no code examples. Configuration page has minimal troubleshooting guidance. No explanation of idempotency or what happens when the same message is delivered twice.

---

#### Wolverine (JasperFx)

**Durability documentation:** [wolverinefx.net/guide/durability/](https://wolverinefx.net/guide/durability/)

Wolverine uses the umbrella term "Durable Messaging" rather than "reliability." Their framing emphasizes that endpoints must be explicitly marked durable — unlike Mocha where it is additive middleware — and positions durability as the layer that survives application restarts. They describe the background relay process (TPL Dataflow queues) in more implementation detail than Mocha does.

**EF Core outbox:** [wolverinefx.net/guide/durability/efcore/outbox-and-inbox](https://wolverinefx.net/guide/durability/efcore/outbox-and-inbox)

Wolverine is unique in that they document both the **inbox** and **outbox** as a pair. The inbox is the consumer-side mechanism: incoming messages are persisted to the database so that if processing crashes mid-handler, the message can be replayed without re-delivery from the broker. This inbox+outbox pairing is what makes Wolverine's durability story about coordinated exactly-once processing rather than just at-least-once delivery.

The three integration contexts (Wolverine handler, MVC controller, Minimal API) are each shown with full code examples, which is more thorough than Mocha's single handler example.

**Dead letter storage:** [wolverinefx.net/guide/durability/dead-letter-storage.html](https://wolverinefx.net/guide/durability/dead-letter-storage.html)

Wolverine stores dead letters in a `wolverine_dead_letters` database table and provides a **REST API** for querying, replaying, and expiring dead letters with filters by message type, exception type, tenant ID, and date range. This operational tooling is a significant differentiator and shows production-grade thinking that Mocha's dead-letter section does not address.

**Error handling:** [wolverinefx.net/guide/handlers/error-handling](https://wolverinefx.net/guide/handlers/error-handling)

Wolverine's error handling covers Immediate Retry, Cooldown Retry, Scheduled Retry, Requeue, and Dead Letter with both attribute-based and fluent API configuration. The per-endpoint circuit breaker is documented separately from the global retry policy. One notable claim in their docs: Wolverine deliberately removed Polly as a dependency due to "diamond dependency conflicts," instead reimplementing the patterns. This is the opposite of Mocha (which uses Polly explicitly via `CircuitBreakerStrategyOptions`).

**What Wolverine does well:** Inbox+outbox pairing with clear exactly-once semantics, REST API for dead-letter management, multiple integration contexts (handler, controller, minimal API), explicit endpoint-level durability requirements. Problem-first writing on all durability pages.

**What Wolverine does poorly:** Scattered prerequisites (database requirements mentioned late), minimal diagrams, implicit assumption that readers know Marten/EF Core already, no comparison of at-least-once vs. exactly-once semantics as a framing concept.

---

### Best Practices Found

**1. Name failure modes explicitly**
NServiceBus's "zombie records" and "ghost messages" vocabulary is highly memorable and immediately communicates the stakes. Mocha's current framing ("a successful database commit followed by a failed publish loses the message") is accurate but abstract. Concrete named failure modes in the dual-write section would improve retention.

**2. Diagram the two-phase outbox flow**
Every competitor (NServiceBus, MassTransit) uses a visual diagram to show Phase 1 (write to outbox table) vs Phase 2 (background relay dispatch). Mocha's text description is clear but the absence of any diagram is a gap for visual learners.

**3. Introduce the inbox pattern alongside the outbox**
Wolverine and the microservices.io pattern catalog both treat the outbox and inbox as complementary. The outbox guarantees at-least-once delivery from the publisher side; the inbox (deduplication by message ID) closes the loop on the consumer side. Mocha's documentation leaves consumers with at-least-once guarantees without addressing what that means for handler idempotency.

**4. Split retry tiers into named stages**
NServiceBus's explicit "Immediate Retries" and "Delayed Retries" stages with default values and formulas give developers a mental model before they need to configure anything. Mocha currently does not have a retry mechanism separate from the circuit breaker and fault forwarding. If retries are not a Mocha feature, the documentation should explicitly say why and what the equivalent pattern is (dead-letter + manual replay).

**5. Address idempotency as a consumer responsibility**
The microservices.io canonical reference and MassTransit both note that at-least-once delivery requires idempotent consumers. The current Mocha page guarantees at-least-once delivery via the outbox but does not warn consumers that they may receive the same message more than once, nor does it show a pattern for deduplication.

**6. Document the operational runbook for stuck outbox messages**
Mocha has a good troubleshooting entry for stuck outbox messages. Wolverine goes further with a REST API and database-level replay. MassTransit documents that `OutboxState` is used for delivery ordering and locking across multiple service instances. Adding guidance on how to replay or discard stuck messages operationally (beyond just log-checking) would strengthen Mocha's troubleshooting section.

**7. Distinguish in-memory vs. durable outbox trade-offs**
MassTransit explicitly documents an in-memory outbox for cases where database persistence is not needed or acceptable. Making this choice visible gives developers the right tool for each situation.

---

### External References

**Canonical Pattern References:**

- [Guaranteed Delivery - Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/GuaranteedMessaging.html) — Hohpe & Woolf's original pattern for persistent messaging; the foundation that outbox implements.
- [Dead Letter Channel - Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/DeadLetterChannel.html) — The authoritative description of the dead-letter pattern Mocha implements.
- [Transactional Outbox Pattern - microservices.io](https://microservices.io/patterns/data/transactional-outbox.html) — Chris Richardson's canonical reference, widely cited. Includes the "at-least-once, so consumers must be idempotent" caveat.
- [Idempotent Consumer Pattern - microservices.io](https://microservices.io/patterns/communication-style/idempotent-consumer.html) — The consumer-side complement to the outbox pattern.

**Dual-Write Problem Explanations:**

- [Designing Event-Driven Microservices: The Dual Write Problem - Confluent Developer](https://developer.confluent.io/courses/microservices/the-dual-write-problem/) — Clear course content with diagrams showing a bank deposit scenario where the dual-write problem manifests.
- [Transactional Outbox Pattern - AWS Prescriptive Guidance](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/transactional-outbox.html) — AWS's prescriptive pattern guide covering both relational (outbox table) and NoSQL (CDC via DynamoDB Streams) approaches.

**Competitor Documentation:**

- [NServiceBus Outbox](https://docs.particular.net/nservicebus/outbox/) — Best-in-class framing with "zombie records" and "ghost messages" vocabulary, two-phase flowchart.
- [NServiceBus Architecture: Consistency](https://docs.particular.net/architecture/consistency) — Positions outbox within a broader consistency taxonomy alongside idempotency and workflows.
- [NServiceBus Recoverability](https://docs.particular.net/nservicebus/recoverability/) — Gold standard for retry tier documentation with immediate/delayed split and rate-limiting circuit breaker.
- [MassTransit Transactional Outbox](https://masstransit.io/documentation/patterns/transactional-outbox) — Clear Bus vs. Consumer Outbox distinction with flow diagrams.
- [MassTransit Exceptions](https://masstransit.io/documentation/concepts/exceptions) — Retry type comparison table; `Fault<T>` as a first-class publishable event.
- [Wolverine Durable Messaging](https://wolverinefx.net/guide/durability/) — Store-and-forward framing, explicit endpoint durability requirements.
- [Wolverine Error Handling](https://wolverinefx.net/guide/handlers/error-handling) — Attribute-based and fluent API retry configuration; per-endpoint circuit breakers.
- [Wolverine Dead Letter Storage](https://wolverinefx.net/guide/durability/dead-letter-storage.html) — REST API for operational dead-letter management and replay.
- [Wolverine EF Core Outbox and Inbox](https://wolverinefx.net/guide/durability/efcore/outbox-and-inbox) — Inbox pattern alongside outbox for exactly-once consumer semantics.

---

### Recommendations

**1. Add a "what at-least-once means for your handlers" callout in the outbox section**
After explaining the outbox's delivery guarantee, add a NOTE block stating: "The outbox guarantees at-least-once delivery. Your handlers may be invoked more than once for the same message if the outbox dispatches successfully but the transport acknowledgment is lost before the message is deleted from the outbox table. Design handlers to be idempotent." Link to the idempotent consumer pattern on microservices.io.

**2. Rename failure modes in the dual-write section**
Introduce NServiceBus-style named scenarios: "If the database commits but the publish fails, the event is lost — downstream consumers never see it. If the publish succeeds but the database rolls back, the event describes state that never existed." The named scenario structure does not require NServiceBus's exact words but benefits from their concrete specificity.

**3. Add a two-phase outbox diagram**
An ASCII or Mermaid sequence diagram showing: `Handler -> Outbox Table (same transaction as business data) -> Outbox Processor -> Transport` would be the single highest-value visual addition. MassTransit and NServiceBus both use this to immediately clarify why the pattern works.

**4. Add an Inbox section or cross-reference**
Mocha does not appear to implement an inbox pattern (deduplication table), but the documentation should acknowledge this: "The outbox guarantees at-least-once dispatch. If your handler must process each logical event exactly once, implement consumer-side deduplication by recording processed message IDs in a database table within the same handler transaction." This closes the conceptual loop that every competitor leaves open.

**5. Elevate the troubleshooting section for the outbox**
Add specific guidance on how to manually replay messages stuck in the `OutboxMessages` table (a SQL snippet updating relevant rows, or a CLI command if Mocha provides one), how to inspect `TimesSent` to identify chronically failing dispatches, and what conditions cause dispatch to be skipped entirely. Wolverine's REST API approach is the exemplar here.

**6. Add a "delivery guarantees" comparison table near the outbox section**
A simple table comparing "without outbox: at-most-once publish" vs. "with outbox: at-least-once publish" with notes on what changes at the consumer would serve as an excellent quick reference and is absent from all competitor pages.

**7. Consider splitting the page into two**
All competitors separate retry/error handling (recoverability) from persistence/outbox (durability). The current Mocha page covers both. As Mocha matures and retry behavior is added, the page will become too long. A proactive split into `reliability.md` (circuit breaker, concurrency, fault routing, expiry) and `durability.md` (transactional outbox, at-least-once vs. exactly-once semantics) would match the mental model competitors have trained developers to expect.
