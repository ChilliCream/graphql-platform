# Documentation Navigation Blueprint

## Goals

- Make the reader awesome at building message-driven systems — not just familiar with the API.
- Organize by **problem domain**: readers search by what they're trying to do, not by document category.
- Each topic page combines all writing modes internally (tutorial, how-to, reference, explanation, troubleshooting) so the reader never hunts across pages.
- Make failure behavior explicit — faults, dead-letter, retries, circuit breakers — on every page where they matter.
- Keep every page focused, scannable, and linked to the next logical step.

## Information Architecture

### 1. Home

- `Welcome`
  - What this project is, who it is for, what problems it solves.
  - Keywords: message bus, handlers, transports, reliability.
- `Why This Bus`
  - Positioning, key design choices, non-goals, when to use something else.
  - Keywords: tradeoffs, architecture, use-cases.
- `Release Notes`
  - Versioned changes, breaking changes, migration links.
  - Keywords: changelog, upgrades, compatibility.

### 2. Quick Start

Single page. Takes the reader from zero to "it works": install packages, register the bus, define a handler, send a message, verify output. No page breaks, no "next page" hops.

- **Flow:** Install → `AddMessageBus()` → Implement `IEventHandler<T>` → `.AddEventHandler<T>()` → `.AddInMemory()` → `bus.PublishAsync()` → Verify output
- Keywords: first message, minimal runnable example, DI registration, host bootstrapping.
- Ends with: guided links into Handlers, Messaging Patterns, and Transports for next steps.

### 3. Handlers and Consumers

The first thing a user builds. Handler types, registration, mapping, and the consumer pipeline — all in one place.

Users implement handler interfaces (`IEventHandler<T>`, `IEventRequestHandler<TRequest, TResponse>`, `IEventRequestHandler<TRequest>`) and register them with the builder. The bus wraps these into consumers automatically. This page covers both sides: the handler interfaces users implement, and the consumer infrastructure that runs them.

- **Tutorial section:** Implement `IEventHandler<T>`, register it with `.AddEventHandler<T>()`, and verify it executes.
- **How-to section:** Register request handlers, send handlers, and batch handlers. Map handler interfaces to consumer implementations. Configure per-consumer behavior.
- **Reference section:** `IEventHandler<T>`, `IEventRequestHandler<TRequest, TResponse>`, `IEventRequestHandler<TRequest>`, `IBatchEventHandler`, `IConsumer<T>` contracts. Consumer configuration options. Builder registration methods.
- **Explanation section:** How handlers map to consumers (SubscribeConsumer, RequestConsumer, SendConsumer, BatchConsumer). Consumer roles and semantics. How the consumer pipeline compiles and executes.
- **Troubleshooting section:** Handler not invoked, mapping mismatches, consumer pipeline errors.
- Keywords: IEventHandler, IEventRequestHandler, handler registration, consumer mapping, consumer pipeline, execution chain.

### 4. Messaging Patterns

The three core patterns in one place: events (pub/sub), commands (send), and request/reply. These map directly to the three handler interfaces and the three `IMessageBus` methods (`PublishAsync`, `SendAsync`, `RequestAsync`).

- **Events (pub/sub) section:**

  - Tutorial: Publish an event and handle it in multiple consumers.
  - How-to: Configure fan-out, filter events, manage multiple subscriptions.
  - Reference: `IEventHandler<T>`, `PublishAsync`, event routing options.
  - Keywords: pub/sub, fan-out, subscribe, multi-consumer.

- **Commands (send) section:**

  - Tutorial: Send a command and handle acknowledgment.
  - How-to: Implement idempotent command handlers, configure delivery guarantees.
  - Reference: `IEventRequestHandler<TRequest>`, `SendAsync`, acknowledgment options.
  - Keywords: send, acknowledgment, command semantics, idempotency.

- **Request/Reply section:**

  - Tutorial: Build a complete request/reply flow from scratch.
  - How-to: Handle response faults, set timeouts, correlate requests.
  - Reference: `IEventRequestHandler<TRequest, TResponse>`, `RequestAsync`, promise completion API, reply context.
  - Keywords: request/response, promise completion, correlation, faults.

- **Explanation section:** When to use publish vs. send vs. request. How correlation works. Why reply endpoints exist. Command vs. event semantics.
- **Troubleshooting section:** Events not received, duplicate delivery, timeouts, missing replies, lost commands.

### 5. Messages

The wire format: envelopes, headers, correlation IDs, content types, serialization contracts, message identity. Most users don't touch this directly until they need custom headers or non-default serialization — the bus handles it automatically.

- **Tutorial section:** Inspect a message envelope and its headers in a running system.
- **How-to section:** Set custom headers, configure serialization, work with content types.
- **Reference section:** `MessageEnvelope` fields (MessageId, CorrelationId, ConversationId, CausationId, SourceAddress, DestinationAddress, ResponseAddress, FaultAddress, ContentType, MessageType, DeliverBy, Headers), serialization options.
- **Explanation section:** Why envelopes wrap messages, design rationale for headers and correlation, how message types are resolved.
- **Troubleshooting section:** Serialization failures, missing headers, content-type mismatches.
- Keywords: envelope, metadata, serialization, headers, correlation, content type, MessageEnvelope.

### 6. Transports

Overview page plus one sub-page per transport.

- `Transport Overview`
  - Transport abstraction, shared vs. transport-specific behavior, how to override middleware/features per transport.
  - Keywords: contracts, capabilities, scope override, precedence.
- `InMemory Transport`
  - Setup, use-cases, limitations.
  - Keywords: local dev, testing.
- `RabbitMQ Transport`
  - Setup, broker configuration, reply endpoints, operational notes.
  - Keywords: broker config, reply endpoints.
- `Postgres Transport`
  - Setup, polling/processing model, operational notes.
  - Keywords: DB transport, polling model.

Each transport sub-page follows the same internal structure: tutorial (get it running), how-to (configure for production), reference (all options), troubleshooting (common issues).

### 7. Routing and Endpoints

Routes, topology, bindings, and endpoint configuration. Most users rely on conventions — this page explains what the conventions do and how to override them.

- **Tutorial section:** Verify endpoint discovery with default conventions, then customize a route.
- **How-to section:** Configure endpoint naming, bindings, explicit vs. implicit consumer binding, override per-endpoint features.
- **Reference section:** Route types, endpoint options, binding conventions, features and scopes (bus/transport/endpoint precedence), `IBusNamingConventions`.
- **Explanation section:** The routing model, how topology is built, why scoped features exist, convention defaults (queue names for requests, topic names for events).
- **Troubleshooting section:** Missing routes, binding conflicts, endpoint discovery failures.
- Keywords: routing model, topology, bindings, endpoint conventions, features, scopes, override hierarchy.

### 8. Middleware and Pipelines

Pipeline architecture, the built-in middleware catalog, and writing custom middleware.

- **Tutorial section:** Understand how dispatch, receive, and consumer pipelines work (include a Mermaid diagram).
- **How-to section:** Toggle built-in middleware, adjust ordering, write custom middleware.
- **Reference section:** Built-in middleware catalog (one subsection per middleware with inputs, outputs, failure semantics), middleware factory API, ordering semantics.
- **Explanation section:** How the middleware compiler works, why three separate pipelines exist, design rationale for ordering.
- **Troubleshooting section:** Middleware not executing, incorrect ordering, pipeline compilation errors.
- Keywords: middleware compiler, ordering, pipeline architecture, dispatch pipeline, receive pipeline, consumer pipeline.

### 9. Reliability

Faults, dead-letter, expiry, concurrency, circuit breakers, and the transactional outbox.

- **Tutorial section:** Configure basic fault handling and verify dead-letter behavior.
- **How-to section:** Set up circuit breakers, configure concurrency limits, tune delivery windows, enable the transactional outbox.
- **Reference section:** Fault headers, dead-letter options, DeliverBy semantics, concurrency semaphore config, circuit breaker scopes and cooldown, outbox options and persistence providers.
- **Explanation section:** Delivery guarantees and boundaries, the failure model, why transport-scoped vs. receive-scoped breakers exist, why the outbox pattern exists, consistency boundaries.
- **Troubleshooting section:** Poison messages, hot failure loops, stale commands, breaker stuck open, messages stuck in outbox, duplicate delivery.
- Keywords: fault handling, dead-letter, TTL, concurrency, circuit breakers, backpressure, delivery guarantees, transactional outbox, consistency.

### 10. Sagas

State machine orchestration for long-running processes that coordinate multiple messages.

- **Tutorial section:** Build a simple saga that coordinates two messages.
- **How-to section:** Manage saga state, handle compensation, correlate saga messages, configure saga persistence.
- **Reference section:** `Saga` base class, `SagaState`, `ISagaStore`, `ISagaTransaction`, saga descriptor API, state transitions, timeout events.
- **Explanation section:** When to use sagas vs. simple handlers, orchestration vs. choreography tradeoffs, how saga state machines work.
- **Troubleshooting section:** Saga state not persisted, correlation mismatches, stuck sagas, timeout not firing.
- Keywords: state machine, orchestration, long-running processes, compensation, SagaState.

### 11. Observability

Tracing, metrics, logging, and troubleshooting — all on one page.

- **Tutorial section:** Enable tracing and verify spans appear.
- **How-to section:** Propagate correlation headers, configure metrics collection, set log levels.
- **Reference section:** Trace headers, metric names and interpretation, log events and levels, observer hooks, `IBusDiagnosticObserver`.
- **Explanation section:** The diagnostic model, how trace propagation works across messages, OpenTelemetry semantic conventions.
- **Troubleshooting section:** Missing spans, metrics not reporting, log noise, symptom-to-cause playbooks.
- Keywords: distributed tracing, metrics, logging, diagnostics, correlation, spans, OpenTelemetry.

### 12. Testing

How to test message-driven code using the InMemory transport and test harness patterns.

- **Tutorial section:** Write a test that publishes an event and asserts the handler ran.
- **How-to section:** Test request/reply flows, assert message content, verify saga state transitions, test middleware behavior.
- **Reference section:** InMemory transport test configuration, test harness utilities, assertion patterns.
- **Explanation section:** Why InMemory is the recommended test transport, isolation strategies, integration vs. unit testing tradeoffs.
- **Troubleshooting section:** Handler not invoked in tests, timing issues, test isolation failures.
- Keywords: InMemory transport, test harness, integration testing, unit testing, test isolation.

## Page Best Practices

Applies to every page:

1. **Start with what and when.** Open with what this topic is and when the reader needs it.
2. **Include the why.** Explain the design intent — why this exists, not just what it does.
3. **Show failure behavior.** What happens if this is misconfigured or missing.
4. **Lead with code, follow with explanation.** Show a minimal working example first, then explain it. Developers read code blocks first.
5. **Show expected output.** After every code example, show what the reader should see so they can verify they're on track.
6. **Progressive disclosure.** Start with the simplest version, then layer in complexity. Use clearly marked deep-dive subsections for advanced details.
7. **Build mental models.** Use analogies to familiar patterns (e.g., "If you've used middleware in ASP.NET Core, this pipeline works similarly"). Include Mermaid diagrams for pipelines and flows where they clarify structure better than prose.
8. **Include troubleshooting.** Every page where the reader can get stuck needs a troubleshooting section: exact error message as heading, then cause, then fix.
9. **Bridge to next steps.** End with related pages and a clear path forward — never leave the reader at a dead end.
10. **Stay focused.** One topic per page. Long is fine; unfocused is not.

## Writing Style Guide

Follow these rules for consistent voice and language across all pages. See `docs-writer.md` for the full rationale behind each rule.

### Tone

- **Conversational but not cutesy.** Write like a smart colleague at a whiteboard — not a textbook, not a marketing page.
- **Confident but not arrogant.** State things directly. No hedging ("you might want to perhaps consider...").
- **Friendly but not fluffy.** Zero filler. Every sentence earns its place.

### Person and Voice

- **Second person.** "You configure this by..." not "The user can configure this by..."
- **Active voice.** "Run the migration" not "The migration should be run."
- **Present tense.** "This returns a list" not "This will return a list."
- **Imperative mood for instructions.** "Install the package" not "You should install the package."
- **Conditional before instruction.** "To enable caching, add the following middleware" — lead with the why, then the what.

### Clarity

- **Short sentences.** If a sentence has a comma and an "and" and another clause — split it.
- **One idea per paragraph.** 2-4 sentences max.
- **Plain language.** "Use" not "utilize". "Start" not "initiate". "About" not "approximately".
- **Be precise with words.** Never write "just", "simply", "easy", or "obvious" — these are dismissive if the reader is struggling.
- **Front-load everything.** Put the most important word first in headings, paragraphs, and list items. "Configure authentication for endpoints" not "A guide to configuring authentication."
- **Define jargon on first use** or link to the Terminology table below.
- **Write for a global audience.** No idioms, no culturally specific references, no humor that doesn't translate.

### Headings

- Make headings task-oriented and descriptive. The right sidebar builds its table of contents from headings — make every one useful for jumping to.
- Examples: "Register services with the builder API" not "Builder API". "Understand how pipelines work" not "Pipeline Architecture".

### Formatting for Scannability

- **Lead with code, follow with explanation.** Developers scan code blocks first, headings second, prose last.
- **Code examples must be:** copy-pasteable, minimal, annotated with inline comments where non-obvious, and showing expected output.
- **Multi-representation tabs.** When a concept can be shown from multiple angles (e.g., configuration vs. code), use tabbed code blocks.
- **Use tables** for option/parameter lists and comparisons.
- **Use admonitions sparingly.** Warnings (will break things), tips (non-obvious shortcuts), notes (important context). If everything is highlighted, nothing is.
- **Use Mermaid diagrams** where they clarify flow or structure better than prose. Don't restate in prose what a clear diagram already shows.
- **Link liberally.** Connect related concepts across pages and within the same page.

### Consistency

- **Terminology:** Use one term per concept everywhere (see Terminology table below).
- **Formatting patterns:** If one CLI flag uses `--flag=value`, all flags use that format.
- **Code style:** Match the project's actual code style in every example.
- **Capitalization:** Be consistent with product names, feature names, and headings.

### Section-Type Rules

| Mode                | Key Rules                                                                                                                                                |
| ------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Tutorial**        | One complete working example end-to-end. You are responsible for the reader's success. Explain only what's needed in the moment.                         |
| **How-To**          | Title format: "How to [verb] [thing]". Assume competence. Numbered steps, one action per step. Include gotchas inline.                                   |
| **Reference**       | Structure mirrors the codebase. Every parameter: name, type, default, description, example. No narrative. Add a Usage subsection with practical recipes. |
| **Explanation**     | Answer "why" and "how it works under the hood." Use analogies and diagrams. Opinions are welcome here.                                                   |
| **Troubleshooting** | Structure as problem, cause, solution. Use the exact error message as the heading. Cover the 3-5 most common issues.                                     |

## Terminology

Use these canonical terms consistently. Do not alternate between synonyms for the same concept.

| Canonical Term | Do Not Use               | Meaning                                                                                                 |
| -------------- | ------------------------ | ------------------------------------------------------------------------------------------------------- |
| consumer       | handler (alone)          | A class that processes a message. "Handler" may refer to the method; "consumer" is the registered unit. |
| dispatch       | send (generic)           | The act of putting a message onto the bus. Use "send" only for command-style one-way delivery.          |
| publish        | broadcast                | Emit an event to all subscribers.                                                                       |
| send           | dispatch (generic)       | Deliver a command to a single endpoint.                                                                 |
| envelope       | wrapper, container       | The metadata structure that wraps a message body.                                                       |
| endpoint       | destination, queue       | A named receive location for messages.                                                                  |
| route          | binding, mapping (alone) | The link between a message type and an endpoint.                                                        |
| transport      | provider, broker (alone) | The infrastructure layer that moves messages (RabbitMQ, Postgres, InMemory).                            |
| feature        | setting (alone)          | A configurable behavior scoped to bus, transport, or endpoint.                                          |
| fault          | error (alone)            | A structured failure response in the request/reply pattern. Use "error" for general exceptions.         |

## Pre-Publication Checklist

Before finalizing any page, verify:

- [ ] Can a new user go from zero to "it works" in under 5 minutes with the Quick Start?
- [ ] Does this page answer one clear topic?
- [ ] Are all code examples tested, runnable, and showing expected output?
- [ ] Is there a clear next step at the end of the page?
- [ ] Are prerequisites stated upfront?
- [ ] Are error cases and troubleshooting covered?
- [ ] Is the language free of "just", "simply", "easy"?
- [ ] Would a non-native English speaker understand this clearly?
- [ ] Can a beginner follow top-to-bottom without reading other pages first?
- [ ] Can an expert find what they need via the right sidebar without reading beginner content?

## Suggested Authoring Order

1. Quick Start
2. Handlers and Consumers
3. Messaging Patterns
4. Messages
5. Transports
6. Routing and Endpoints
7. Middleware and Pipelines
8. Reliability
9. Sagas
10. Observability
11. Testing

## External Documentation Exemplars

Use these as structural references — never copy wording.

### Home

- MassTransit docs landing: https://masstransit.io/documentation
- Wolverine guide landing: https://wolverinefx.net/guide/

### Quick Start

- MassTransit quick start flow: https://masstransit.io/documentation/configuration
- EasyNetQ onboarding style: https://easynetq.com/

### Handlers and Consumers

- MassTransit consumers: https://masstransit.io/documentation/configuration/consumers
- NServiceBus handlers and sagas: https://docs.particular.net/nservicebus/handlers-and-sagas
- Wolverine handlers: https://wolverinefx.net/guide/messaging/

### Messaging Patterns

- MassTransit requests: https://masstransit.io/documentation/concepts/requests
- NServiceBus messaging: https://docs.particular.net/nservicebus/messaging/
- Wolverine messaging concepts: https://wolverinefx.net/guide/messaging/

### Messages

- MassTransit messages: https://masstransit.io/documentation/concepts/messages

### Routing and Endpoints

- MassTransit configuration model: https://masstransit.io/documentation/configuration
- Wolverine host/config patterns: https://wolverinefx.net/guide/hostbuilder/

### Transports

- MassTransit transport index: https://masstransit.io/documentation/transports
- Wolverine RabbitMQ transport: https://wolverinefx.net/guide/messaging/transports/rabbitmq/
- Wolverine PostgreSQL transport: https://wolverinefx.net/guide/messaging/transports/postgresql/

### Middleware and Pipelines

- Wolverine handler middleware: https://wolverinefx.net/guide/handlers/middleware
- Wolverine runtime/pipeline model: https://wolverinefx.net/guide/runtime/
- MassTransit middleware (filters): https://masstransit.io/documentation/configuration/middleware

### Reliability

- MassTransit exceptions/retry/faulting: https://masstransit.io/documentation/concepts/exceptions
- MassTransit transactional outbox: https://masstransit.io/documentation/patterns/transactional-outbox
- NServiceBus recoverability: https://docs.particular.net/nservicebus/recoverability/
- NServiceBus recoverability architecture: https://docs.particular.net/architecture/recoverability
- Brighter outbox/inbox patterns: https://brightercommand.gitbook.io/paramore-brighter-documentation/outbox-and-inbox/brighteroutboxsupport

### Observability

- MassTransit observability: https://masstransit.io/documentation/configuration/observability
- NServiceBus monitoring/operations: https://docs.particular.net/monitoring/
- Wolverine diagnostics: https://wolverinefx.net/guide/diagnostics/

### Sagas

- MassTransit sagas: https://masstransit.io/documentation/patterns/saga
- NServiceBus sagas: https://docs.particular.net/nservicebus/handlers-and-sagas

### Testing

- MassTransit testing: https://masstransit.io/documentation/concepts/testing
- Wolverine testing: https://wolverinefx.net/guide/testing/

### Usage Rule

1. Use MassTransit as the primary structure benchmark.
2. Use Wolverine and NServiceBus for section-level depth examples.
3. Never copy wording; only reference information architecture patterns.
