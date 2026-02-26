Now I have comprehensive research. Let me compile the final structured report.

---

## Research Report: Saga Documentation Analysis

### Current Page Summary

**File:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/sagas.md`

**Structure (669 lines):**

1. One-paragraph concept introduction (lines 6-12)
2. Quick-start tutorial: "Build a quick refund saga" -- walks through state definition, message contracts, saga class, registration, and triggering (lines 14-233)
3. How-to guides section covering 7 patterns:
   - Parallel operations coordination
   - Event-initiated sagas
   - Publishing events from transitions
   - On-entry actions
   - Fault handling and compensation
   - Postgres persistence
   - Fluent API (inline sagas)
4. Full API reference tables: `ISagaDescriptor`, `ISagaStateDescriptor`, `ISagaTransitionDescriptor`, `ISagaFinalStateDescriptor`, `SagaStateBase`, `ISagaStore`, `ISagaTransaction`
5. Correlation mechanics explanation (ICorrelatable, header-based, lookup order)
6. "When to use sagas vs. handlers" decision table
7. Troubleshooting section (4 common errors)

**Approach:** Example-first (shows working code before explaining concepts), tutorial-led, API-complete.

**What's present:** Working refund saga from end to end, parallel coordination pattern, fault/compensation, persistence setup, API tables, troubleshooting.

**What's absent:** No state machine diagram. No mention of the saga pattern's theoretical origin (Garcia-Molina & Salem 1987). No saga vs. process manager distinction. No discussion of compensating transactions as a concept (only shows `OnFault()` mechanically). No timeout example. No concurrency/race condition discussion. No mention of orchestration vs. choreography distinction.

---

### Competitor Analysis

#### NServiceBus (Particular Software) -- Gold Standard

**URLs:** https://docs.particular.net/nservicebus/sagas/ and https://docs.particular.net/tutorials/nservicebus-sagas/

**Approach:** Tutorials and reference documentation are separated. The tutorial (`/tutorials/nservicebus-sagas/`) uses problem-first pedagogy: it opens by describing a real shipping workflow that breaks with plain handlers, then rebuilds it as a saga. The reference docs (`/nservicebus/sagas/`) are encyclopedic. They have dedicated sub-pages for:

- Message correlation (`/sagas/message-correlation`)
- Saga concurrency (`/sagas/concurrency`)
- Complex saga finding (`/sagas/saga-finding`)
- Saga audit and visualization (via ServicePulse)
- Saga scenario testing (`/nservicebus/testing/saga-scenario-testing`)

**Theory introduction:** NServiceBus explicitly states "A saga should be only a message-driven state machine" and directly references the Process Manager pattern from Enterprise Integration Patterns. They distinguish NServiceBus sagas (a programming model) from the academic "saga distributed transactions pattern" -- and note their feature can implement the latter.

**State transitions:** The tutorial reframes sagas as "policies" (e.g., `ShippingPolicy`) to make the purpose intuitive. It explicitly challenges the assumption that messages arrive in order ("Not so fast!"), using misconception correction as a teaching device.

**Diagrams:** The core docs have _no_ state machine diagrams in text. However, NServiceBus compensates with the **ServicePulse Saga Diagram** -- a runtime visualization tool that shows saga lifecycle in a three-column layout (incoming messages | state diffs | outgoing messages) with timestamps and property change diffs. This is tooling-as-documentation.

**Persistence:** Covered in a separate persistence sub-section. They support pessimistic and optimistic locking strategies and document which persisters use which approach explicitly.

**Concurrency:** Dedicated page. Covers pessimistic vs. optimistic strategies, recommends hosting sagas in dedicated endpoints, documents scatter-gather sub-saga patterns for high-load scenarios.

**Testing:** The `TestableSaga` class is fully documented with scenario-level testing (not just unit testing). Supports virtual time advancement for timeout testing, `SimulateReply` for mocking external responses, and snapshot assertions of saga state.

**What NServiceBus does WELL:**

- Separation of tutorial vs. reference concerns
- Explicit anti-patterns: "A saga must not perform any I/O operations" is a hard rule, clearly stated
- Problem-first narrative in tutorials
- Misconception correction baked into teaching
- Dedicated concurrency page with architectural recommendations
- Tooling (ServicePulse saga diagram) that makes state visible at runtime
- Comprehensive testing documentation

**What NServiceBus does POORLY:**

- No visual state machine diagrams in the core documentation
- The main saga page can feel encyclopedic without a clear learning path
- Correlation docs are a separate page (creates navigation friction)

---

#### MassTransit

**URLs:** https://masstransit.io/documentation/patterns/saga and https://masstransit.io/documentation/patterns/saga/state-machine

**Approach:** Theory-first on the state machine page. Opens with component definitions (State Machine, Instance, State, Event, Behavior) before showing any code. Grounds the pattern in academic history ("original Princeton paper," Arnon Rotem-Gal-Oz's work) -- this is the only .NET framework that cites original sources.

**State transitions:** The `During()` / `Initially()` / `When()` / `TransitionTo()` DSL is documented sequentially with code examples for each activity type (Publish, Send, Respond, Schedule, Request). Coverage is comprehensive but dense -- the page compresses a lot into code blocks with minimal narrative between them.

**Diagrams:** None present on any page. The documentation relies entirely on code and text.

**Persistence:** Addressed in a separate section (Entity Framework, MongoDB, Redis, etc.). The state machine page itself barely touches it.

**Concurrency:** Not prominently documented. A known gap.

**Correlation:** The most detailed in any .NET framework. Covers `CorrelatedBy<Guid>`, `CorrelateById()`, `CorrelateBy()` with expressions, global topology configuration, missing instance handling with `OnMissingInstance()`. This is a strength.

**Request/Response in sagas:** Documented via the `Request<TSaga, TRequest, TResponse>` property pattern. Covers three response outcomes: `Completed`, `TimeoutExpired`, `Faulted`. Time-to-live and timeout configuration shown with code.

**What MassTransit does WELL:**

- Academic grounding (cites original papers)
- Most thorough correlation documentation of any framework
- Explicit three-outcome model for request/response (completed, timeout, fault)
- `MissingInstance` handler pattern for resilience edge cases

**What MassTransit does POORLY:**

- No diagrams at all -- state machine DSL is complex enough that diagrams would significantly help
- Theory-first approach on the state machine page can be hard to follow without a running example to anchor to
- Transitions between intro and advanced topics are uneven -- advanced sections compress too much into example-dense blocks
- Persistence and concurrency documentation is fragmented and harder to find

---

#### Wolverine (JasperFx)

**URL:** https://wolverinefx.net/guide/durability/sagas.html

**Approach:** Pragmatic, convention-over-configuration. Opens by acknowledging the "saga vs. process manager" semantic debate and dismissing it: "we're just not going to get hung up on 'process manager' vs. 'saga' here." The documentation leads with a working `OrderSaga` example before explaining mechanics.

**What's unique:** Wolverine uses method-naming conventions rather than DSL configuration. A method named `Start` creates a new saga. A method named `Handle` processes messages on an existing saga. `NotFound` suppresses missing-instance exceptions. This convention approach dramatically reduces boilerplate but requires understanding the implicit routing rules.

**State transitions:** Explained through method conventions rather than explicit state declarations. There is no `TransitionTo()` concept -- state is just data in the saga object. The framework regenerates the handler wrapper code and shows it explicitly in the docs ("Here is the generated code") to demystify the framework's behavior.

**Diagrams:** None.

**Persistence:** Covers Marten, EF Core, and lightweight SQL-backed storage (added in v3). The lightweight option (no document database required) is a differentiator.

**Concurrency:** Mentioned briefly with optimistic checks. Not deeply covered.

**Timeouts:** Uses `TimeoutMessage` subclassing pattern. Well-documented with example.

**What Wolverine does WELL:**

- Least ceremony approach -- genuinely simpler to write sagas
- Showing generated code demystifies magic and builds trust
- `TimeoutMessage` abstraction is clean and well-documented
- Explicit "not found" handling prevents silent failures
- Separated mode for multiple sagas handling identical messages (a unique feature)

**What Wolverine does POORLY:**

- No diagrams
- The method-naming convention approach is powerful but requires learning the implicit rules -- docs cover the rules but don't explain the mental model deeply
- Correlation documentation is thin -- relies heavily on the `{SagaType}Id` convention without explaining failure cases
- No dedicated concurrency page

---

### Best Practices Found

**1. Separate tutorial from reference (NServiceBus)**
The tutorial should answer "how do I build my first saga?" The reference should answer "what does method X do?" Mixing these creates pages that serve neither audience well.

**2. Problem-first introduction is superior to theory-first (NServiceBus tutorial)**
Start with a real business problem that plain handlers cannot solve. Show why the saga pattern is necessary before introducing the mechanics. MassTransit's theory-first approach on the state machine page requires more cognitive load upfront.

**3. Explicitly state anti-patterns (NServiceBus)**
"A saga must not perform I/O" is a hard constraint that prevents a whole class of bugs. Stating what sagas _cannot_ do is as important as stating what they can do.

**4. Address the "messages arrive out of order" misconception early (NServiceBus tutorial)**
This is the most common incorrect assumption beginners make. The current Mocha page does not address it.

**5. Timeout and compensation need equal prominence (MassTransit, NServiceBus)**
The current Mocha page covers `OnFault()` mechanically but does not explain the concept of compensating transactions or why they matter. MassTransit's three-outcome model (completed/timeout/faulted) is cleaner than Mocha's implicit assumption that replies always arrive.

**6. Correlation deserves its own section or sub-page (NServiceBus, MassTransit)**
Both competitors dedicate substantial content to correlation. Mocha covers it in a brief "How saga correlation works" section but doesn't address edge cases (missing instances, correlation on non-Guid types, correlation failures).

**7. State machine diagrams are a documentation gap across all .NET frameworks**
None of the three competitors include inline state machine diagrams. This is consistently cited as a weakness in community feedback. A simple Mermaid state diagram (states as nodes, transitions as labeled edges) in the Mocha docs would make Mocha the clear leader in this respect.

**8. Academic and pattern-theory grounding adds authority (MassTransit, NServiceBus)**
MassTransit cites the Princeton paper. NServiceBus links to Hohpe & Woolf's Process Manager pattern. Mocha has none of this. Adding a brief acknowledgment of the original pattern origin grounds the feature in established computer science.

**9. Testing documentation is a major gap in all frameworks except NServiceBus**
NServiceBus's `TestableSaga` with scenario-level testing, virtual time, and snapshot assertions is significantly more mature than anything MassTransit or Wolverine offer. Mocha's testing page should cover sagas explicitly.

**10. Saga vs. handler decision guidance (Mocha does this well)**
The "when to use sagas vs. handlers" table at the end of the Mocha page is actually better than what any of the three competitors provide. NServiceBus buries this guidance. MassTransit doesn't have it. Wolverine lacks it. This is a strength of the current Mocha page.

---

### External References (Quality URLs)

**Foundational academic source:**

- Original paper: https://dl.acm.org/doi/10.1145/38713.38742 -- Garcia-Molina & Salem, "Sagas," ACM SIGMOD 1987. The origin of the pattern.
- Paper summary accessible without paywall: https://dominik-tornow.medium.com/paper-summary-sagas-395ef2a9a575

**Enterprise Integration Patterns (Hohpe & Woolf):**

- Process Manager pattern: https://www.enterpriseintegrationpatterns.com/patterns/messaging/ProcessManager.html -- NServiceBus explicitly links to this. Their sagas implement the Process Manager pattern.

**Chris Richardson's authoritative microservices patterns reference:**

- https://microservices.io/patterns/data/saga.html -- covers choreography vs. orchestration, compensating transactions, lack of isolation (the 'I' in ACID), and the transactional outbox requirement. Widely cited as the definitive web reference.

**Microsoft Azure Architecture Center:**

- https://learn.microsoft.com/en-us/azure/architecture/patterns/saga -- covers compensable/pivot/retryable transaction classification, data anomalies (lost updates, dirty reads, fuzzy reads), and countermeasures. Has actual architecture diagrams showing orchestration and choreography flows. The taxonomy of "compensable," "pivot," and "retryable" transactions is particularly useful.

**NServiceBus saga tutorial (gold standard tutorial format):**

- https://docs.particular.net/tutorials/nservicebus-sagas/1-saga-basics/

**NServiceBus concurrency reference:**

- https://docs.particular.net/nservicebus/sagas/concurrency

**MassTransit state machine reference:**

- https://masstransit.io/documentation/patterns/saga/state-machine

**Wolverine sagas:**

- https://wolverinefx.net/guide/durability/sagas.html

**InfoQ article on saga + outbox pattern (good real-world implementation reference):**

- https://www.infoq.com/articles/saga-orchestration-outbox/

---

### Recommendations

**1. Add a state machine diagram (highest impact, no competitor does this)**
Add a Mermaid diagram above the tutorial showing states as nodes and message-triggered transitions as labeled edges for the refund saga. Something like:

```
[Initial] --RequestQuickRefundRequest--> [AwaitingRefund] --ProcessRefundResponse--> [Completed]
```

And for the parallel saga:

```
[Initial] --ReturnPackageReceivedEvent--> [AwaitingInspection] --InspectReturnResponse--> [AwaitingBothReplies]
[AwaitingBothReplies] --RestockInventoryResponse--> [RestockDoneAwaitingRefund]
[AwaitingBothReplies] --ProcessRefundResponse--> [RefundDoneAwaitingRestock]
[RestockDoneAwaitingRefund] --ProcessRefundResponse--> [Completed]
[RefundDoneAwaitingRestock] --RestockInventoryResponse--> [Completed]
```

This single addition would make Mocha's saga documentation immediately clearer than any competitor's.

**2. Add a brief theoretical grounding paragraph**
After the opening paragraph, add 2-3 sentences acknowledging the Garcia-Molina & Salem 1987 origin, NServiceBus's framing of sagas as Process Manager implementations (per Hohpe & Woolf), and the choreography/orchestration distinction -- then clearly state Mocha sagas implement orchestration-style coordination. Link to `microservices.io/patterns/data/saga.html` and the EIP Process Manager page.

**3. Address message ordering assumption explicitly**
Add a callout warning: messages do not arrive in order. If a saga handles `OrderPlaced` and `PaymentReceived`, both message types must be configured to initiate the saga (`Initially()`) because either may arrive first. NServiceBus's tutorial treats this as a key teaching moment.

**4. Expand the timeout section**
The current `OnTimeout()` entry appears only in the reference table (`ISagaStateDescriptor`) with one line of description. MassTransit devotes substantial documentation to timeout configuration. Add a how-to guide: "Schedule a timeout" showing `OnTimeout()` with `SagaTimedOutEvent`, how to schedule and cancel timeouts, and what to do when a timeout fires (transition to a compensation state or complete).

**5. Expand correlation edge cases**
The current "How saga correlation works" section explains the happy path but omits: what happens when a saga instance is not found for a reply (silently dropped? exception?), how to handle non-reply events that must correlate by business key, and how to debug correlation mismatches beyond the troubleshooting bullets.

**6. Add a compensating transactions concept section**
Before the `Handle faults and compensation` how-to, add a short conceptual introduction explaining what compensating transactions are (the academic term), why the lack of ACID isolation across services means you need them (reference the Microsoft Azure Architecture page taxonomy), and how `OnFault()` + `Send(compensation)` maps to that pattern. Currently the how-to is purely mechanical without the conceptual framing.

**7. Consider a concurrency callout**
NServiceBus dedicates a full page to saga concurrency. The current Mocha page mentions "optimistic concurrency with a version column" once in the persistence section. A callout or short section explaining what happens when two messages for the same saga arrive simultaneously (one succeeds, one retries) -- and the recommendation to avoid very high concurrency on saga endpoints -- would prevent a category of production issues.

**8. The "when to use sagas vs. handlers" table is a genuine strength**
Keep it, and consider moving it earlier in the document rather than near the end. NServiceBus's tutorial addresses this question implicitly through narrative, but Mocha's explicit table is cleaner and should be positioned where readers encounter it before committing to writing a saga.
