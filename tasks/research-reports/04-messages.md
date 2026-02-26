Now I have a comprehensive picture of all three competitors and the external references. Let me compile the full structured report.

---

## Research Report: Mocha Messages Page vs. Competitor Documentation

### Current Page Summary

**File:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/messages.md`

**What it covers:**

The page is scoped to a specific and narrow concern: the envelope/wire format, not message type design. It explains that every message is wrapped in an envelope and covers:

1. A tutorial walkthrough — define a POCO, implement `IConsumer<T>`, register and publish with custom headers.
2. `Set custom headers` — `PublishOptions` and `SendOptions` usage.
3. `Access envelope metadata in a handler` — `IEventHandler<T>` vs `IConsumer<T>` distinction.
4. `Configure message type identity` — URN-based identity, `AddMessage<T>()`.
5. `MessageEnvelope` reference table — 16 properties with type and description.
6. `Headers API reference` — `IReadOnlyHeaders`, `IHeaders`, `HeaderValue` tables.
7. `IConsumeContext<T>` reference — 20-property table.
8. `Why envelopes exist` — conceptual rationale.
9. `How correlation works` — ASCII diagram of `ConversationId / CorrelationId / CausationId`.
10. `How message type resolution works` — URN derivation and polymorphic messages.
11. `Troubleshooting` — three common failure scenarios with fix instructions.

**Structure observation:** The page has a clear conceptual anchor (envelopes and metadata), a task-oriented tutorial at the top, and API reference tables at the bottom. The reference tables are substantial — three separate tables covering `MessageEnvelope`, `IHeaders`, and `IConsumeContext<T>` — which adds weight to the page.

---

### Competitor Analysis

#### NServiceBus (Particular Software)

**Sources:** [Messages, Events, Commands](https://docs.particular.net/nservicebus/messaging/messages-events-commands) | [Message Headers](https://docs.particular.net/nservicebus/messaging/headers) | [Unobtrusive Mode](https://docs.particular.net/nservicebus/messaging/unobtrusive-mode)

**Marker interfaces:** Uses `ICommand`, `IEvent`, `IMessage` as first-class design artifacts. These live in a separate `NServiceBus.MessageInterfaces` package targeting `netstandard2.0` to allow sharing contracts across NServiceBus major versions without forcing upgrade lockstep.

**Serialization:** Minimal inline coverage — treated as a separate concern. Their guidance that following message design rules makes messages "more compatible with serializers" is the extent of inline explanation.

**Versioning:** Explicitly deferred to a separate "evolving contracts" guide. The Messages page itself acknowledges the concern but does not answer it.

**Headers/metadata:** Covered in a dedicated `headers` page with exhaustive tables (transport, serialization, messaging interaction, diagnostic, saga, error/audit, timeout categories). They avoid the word "envelope" entirely — it's all "headers" and "body." Correlation ID vs Conversation ID distinction is explained clearly in prose alongside the table.

**API reference bloat:** Low. The messages page is almost entirely conceptual — principles, comparison table, design guidelines. The header page is reference-heavy but is a distinct page.

**What they do well:**

- The Command vs Event comparison table across 8 dimensions is extremely clear.
- The "why" is front-loaded: marker interfaces communicate intent and enforce routing rules.
- Unobtrusive mode page honestly explains the version coupling problem that marker interfaces create.
- Best-practice enforcement (with disable option) is explicitly called out.

**What they do poorly:**

- Message versioning guidance is absent from the messages page, pointing users elsewhere.
- Generic message types dismissed without workarounds.
- No inline serialization advice.

---

#### MassTransit

**Sources:** [Messages](https://masstransit.io/documentation/concepts/messages) | [Serialization](https://masstransit.io/documentation/configuration/serialization) | [Versioning](https://masstransit.io/architecture/versioning.html)

**Marker interfaces:** Avoids them by default. Messages are plain records, classes, or interfaces. Their stated design principle is "Message design is not object-oriented design." They strongly discourage base classes and polymorphic dispatch hierarchies for messages — "consuming a base class type almost always leads to problems."

**Serialization:** Handled on a separate Serialization page. They use `System.Text.Json` as default with `application/vnd.masstransit+json` content type (enveloped JSON). Mapping between serializer and content type is tabled. However: when to choose which serializer and performance trade-offs are not explained.

**Versioning:** Largely absent from their Messages page. Their versioning architecture page exists but the messages page itself does not link to or summarize it. The `EnclosedMessageTypes` array (which carries all CLR type URNs for polymorphic dispatch) is mentioned in headers documentation but not prominently explained.

**Headers/metadata:** Comprehensive 14-field table: `MessageId`, `CorrelationId`, `RequestId`, `InitiatorId`, `ConversationId`, `SourceAddress`, `DestinationAddress`, `ResponseAddress`, `FaultAddress`, `ExpirationTime`, `SentTime`, `MessageType`, `Host`, and custom headers. Correlation conventions (naming a property `CorrelationId` on the message class for automatic correlation) are explained clearly.

**API reference bloat:** Low on the Messages page — conceptual and naming convention guidance dominate. Reference tables live on separate pages.

**What they do well:**

- "Message design is not object-oriented design" is a memorable, opinionated principle that prevents a common mistake.
- Naming conventions for commands (verb-noun) vs events (noun-verb past tense) are explicit.
- ConsumeContext<T> header table is comprehensive and organized.
- Correlation-by-convention (auto-detecting `CorrelationId` properties) reduces boilerplate.

**What they do poorly:**

- Versioning guidance is almost entirely absent from the message contract documentation.
- Serialization performance trade-offs are unexplained.
- No explicit explanation of the envelope concept — headers exist but "envelope" isn't a concept they surface to users.

---

#### Wolverine (JasperFx)

**Sources:** [Messages and Serialization](https://wolverinefx.net/guide/messages)

**Marker interfaces:** None required. Wolverine explicitly distinguishes itself on this point: "Unlike other .NET messaging or command handling frameworks, there's no requirement for Wolverine messages to be an interface or require any mandatory interface or framework base classes." Optional diagnostic marker attributes exist but serve discovery, not runtime enforcement.

**Serialization:** Best-documented of the three competitors. `System.Text.Json` is default. MessagePack, MemoryPack, and Protobuf are available via separate packages with clear extension method examples. "Self-serializing" messages implementing `ISerializable` for micro-optimization are documented. Serialization is integrated into the Messages page rather than split off.

**Versioning:** Strongest versioning story of the three. The `[MessageIdentity("person-born", Version = 2)]` attribute generates content type `application/vnd.person-born.v2+json`. The `IForwardsTo<T>` pattern allows receiving V1 messages and forwarding them to V2 handlers without duplicating consumer code. This is an explicit, workable versioning strategy explained inline.

**Headers/metadata:** Weak on this page. Correlation IDs, tracing, and message metadata access patterns are largely absent. These are covered in other parts of their docs but not on the messages page, which is surprising given that serialization and versioning are both here.

**API reference bloat:** Well-managed. The page integrates conceptual explanation and code examples together rather than separating into tables.

**What they do well:**

- POCO-first stance is principled and prominently stated.
- Versioning with `[MessageIdentity]` and `IForwardsTo<T>` is the most concrete and actionable of any competitor.
- Multiple serializer options documented with real code, not just a list.
- Self-serializing messages (the `ISerializable` pattern) is a unique advanced capability, well-explained.

**What they do poorly:**

- Headers/metadata access entirely absent from the messages page.
- Interop with non-Wolverine systems mentioned but relegated to a separate tutorial.
- Lower-level serializer configuration options underexplained.

---

### Competitor Comparison Matrix

| Dimension                 | NServiceBus                    | MassTransit           | Wolverine                     | Mocha                      |
| ------------------------- | ------------------------------ | --------------------- | ----------------------------- | -------------------------- |
| Marker interfaces         | Yes (ICommand/IEvent/IMessage) | No (plain types)      | No (optional diagnostic only) | No (plain POCOs)           |
| POCOs emphasized          | Optional (unobtrusive mode)    | Yes                   | Yes, explicitly               | Yes                        |
| Serialization inline      | Minimal                        | Separate page         | Yes, inline                   | Minimal reference          |
| Versioning guidance       | Separate page                  | Absent                | Inline, strongest             | Absent                     |
| Headers/metadata table    | Dedicated page, exhaustive     | ConsumeContext table  | Absent from messages page     | Three inline tables        |
| Envelope concept named    | Never                          | Never                 | Never                         | Yes, front-loaded          |
| API reference volume      | Low (concepts page)            | Low (concepts page)   | Low-medium                    | High (three tables inline) |
| Correlation IDs explained | Yes, prose                     | Yes, convention-based | Absent from this page         | Yes, ASCII diagram         |

---

### Best Practices Found

**1. Separate "message design" from "wire format."** NServiceBus and MassTransit both have dedicated pages for message design (what goes in the message class) and separate pages for headers/wire format. Mocha's `messages.md` currently conflates both, which explains the page weight.

**2. "Message design is not object-oriented design"** (MassTransit) is an excellent opening principle that prevents a class of mistakes before they happen. An equivalent Mocha principle would be: "Your POCO contains business data; the envelope contains infrastructure metadata."

**3. Versioning deserves inline treatment.** Wolverine's `[MessageIdentity]` + `IForwardsTo<T>` pattern is the gold standard. All three competitors have gaps here. Mocha mentions polymorphic messages briefly under message type resolution but gives no guidance on evolving contracts over time.

**4. Naming conventions signal intent.** MassTransit explicitly states: commands = verb-noun (`PlaceOrder`), events = noun-verb past tense (`OrderPlaced`). This is simple and high-value. Mocha's examples already follow this convention but never state it as a rule.

**5. The envelope concept is uniquely Mocha's.** None of the three competitors use the word "envelope" in their user-facing documentation. MassTransit, NServiceBus, and Wolverine all have the concept but surface it as "headers" without a named wrapper. Mocha's explicit envelope framing is a genuine differentiator — it should be leaned into, not buried under API tables.

**6. Correlation explanation by diagram.** Mocha's ASCII diagram of `ConversationId / CorrelationId / CausationId` is more visual than anything any competitor provides. NServiceBus explains these in prose; MassTransit uses convention examples. The diagram is an asset.

**7. API reference tables should live on dedicated pages or be collapsed.** All three competitors avoid heavy inline tables on their concept pages. Mocha has three substantial tables (`MessageEnvelope`, `IHeaders`, `IConsumeContext<T>`) on the same page as the tutorial and conceptual content. This adds approximately 50 lines of reference content that interrupts the narrative.

---

### External References

**Enterprise Integration Patterns (canonical authority):**

- [Message pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/Message.html) — defines the header/body separation that is the foundation of Mocha's envelope.
- [Command Message pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CommandMessage.html) — imperative messages requesting action.
- [Event Message pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/EventMessage.html) — declarative notifications; notably states "many events are empty; their mere occurrence tells the observer to react."
- [Document Message pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/DocumentMessage.html) — data-carrying messages.
- [Message Construction patterns index](https://www.enterpriseintegrationpatterns.com/patterns/messaging/) — full index.

**CloudEvents specification (CNCF standard):**

- [CloudEvents spec](https://github.com/cloudevents/spec/blob/main/cloudevents/spec.md) — the industry-standard envelope attribute set (`id`, `source`, `type`, `time`, `datacontenttype`, extension attributes). Mocha's envelope covers equivalent concerns with different naming. Relevant if Mocha ever needs interop justification.

**Competitor documentation (for ongoing reference):**

- [NServiceBus: Messages, Events, and Commands](https://docs.particular.net/nservicebus/messaging/messages-events-commands)
- [NServiceBus: Message Headers](https://docs.particular.net/nservicebus/messaging/headers)
- [NServiceBus: Unobtrusive Mode](https://docs.particular.net/nservicebus/messaging/unobtrusive-mode)
- [MassTransit: Messages](https://masstransit.io/documentation/concepts/messages)
- [MassTransit: Serialization](https://masstransit.io/documentation/configuration/serialization)
- [Wolverine: Messages and Serialization](https://wolverinefx.net/guide/messages)

---

### Recommendations

**1. Reduce API reference bloat by extracting tables to their own reference page.**
The `MessageEnvelope` (16 properties), `IHeaders`/`IReadOnlyHeaders`/`HeaderValue` (6 methods + struct), and `IConsumeContext<T>` (20 properties) tables are useful but interrupt the conceptual narrative. They should either move to a dedicated "API Reference" page linked from the bottom, or be collapsed under a `<details>` expander. This would cut the current page length by roughly 40%.

**2. Add a "Naming conventions" section (or a callout box).**
None of the competitors codify this rule inline where messages are explained: commands use imperative verb-noun form (`PlaceOrder`, `ProcessPayment`), events use past-tense noun-verb form (`OrderPlaced`, `PaymentProcessed`). Mocha's examples already follow this but the rule is never stated. One sentence or a callout box pays dividends in reducing future user questions.

**3. Add a "Message versioning" section (even a brief one).**
All three competitors either skip this or handle it poorly. Mocha has a gap here too. The minimum viable addition: explain that adding new `init` properties with defaults is backward-compatible, renaming or removing properties is breaking, and that `AddMessage<T>()` can be used to pin the URN when refactoring CLR namespaces. This does not need to be exhaustive — even two paragraphs would put Mocha ahead of every competitor.

**4. Keep the envelope concept front-and-center — it is Mocha's differentiator.**
No competitor names the envelope pattern explicitly. Mocha's "Why envelopes exist" section (currently at position 8 of 11) should be moved higher — ideally to position 2, right after the opening paragraph and before the tutorial. The rationale for envelopes is what makes the tutorial meaningful; readers who understand the "why" follow the "how" more readily.

**5. Tighten the "IEventHandler<T> vs IConsumer<T>" distinction.**
The page explains this correctly but explains it twice — once in the tutorial section and once in the "Access envelope metadata in a handler" section. The duplicate adds ~30 lines. One clear, canonical explanation with the tip callout is sufficient.

**6. Serialization deserves one paragraph, not zero.**
MassTransit and Wolverine both explain their serialization default inline on the messages page. Mocha's page mentions `ContentType` in the envelope table and has a troubleshooting entry for serialization failures, but never states the default. One sentence — "Mocha serializes message bodies as JSON (`application/json`) by default; you can register a custom serializer with `AddSerializer()` on the message type descriptor" — would close this gap.
