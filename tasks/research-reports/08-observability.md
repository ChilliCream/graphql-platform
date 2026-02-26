I now have sufficient information to write a thorough research report. Here is the complete structured report:

---

## Research Report: Mocha Observability Documentation

---

### Current Page Summary

**File:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/observability.md`

**Structure (in order):**

1. Intro paragraph (opt-in, no-op default)
2. "Enable tracing and metrics" section
   - Register instrumentation (code)
   - Subscribe to activity source (code)
   - Verify spans appear (span name list)
3. "How trace context propagates across messages" -- header table + Mermaid sequence diagram
4. "How to configure with .NET Aspire" -- full code block
5. "How to implement a custom diagnostic observer" -- full `CustomDiagnosticObserver` implementation + `IBusDiagnosticObserver` usage explanation
6. "Understand the diagnostic model" -- Mermaid pipeline graph + middleware table + "Why three separate middlewares?" explanation
7. Reference section
   - ActivitySource and Meter table
   - Span attributes table (11 attributes)
   - Metrics table (4 metrics)
   - Queue metrics table (4 metrics)
   - Topic metrics table (2 metrics)
   - Metric dimensions table (10 tags)
   - Trace propagation headers table
   - `IBusDiagnosticObserver` interface reference (full C# interface)
   - Registration method table
8. Troubleshooting section (4 issues with diagnosis + fix)
9. Next steps links

**Length:** ~425 lines -- a long page, among the heaviest in the documentation set.

**Code total:** 6 code blocks including a 33-line custom observer class and a 25-line Aspire configuration block.

**Key factual note from reading the actual source:** Mocha uses **custom non-W3C headers** (`trace-id`, `span-id`, `trace-state`, `parent-id` as separate headers). The W3C standard defines only `traceparent` and `tracestate`. The documentation page does not acknowledge this deviation or explain why these custom headers are used instead of `traceparent`. This is worth calling out as an accuracy concern.

---

### Competitor Analysis

#### NServiceBus

**Source:** [docs.particular.net/nservicebus/operations/opentelemetry](https://docs.particular.net/nservicebus/operations/opentelemetry)

**Strengths:**

- Exhaustive metric listing across three distinct meters (`NServiceBus.Core.Pipeline.Incoming`, `NServiceBus.TransactionalSession`, `NServiceBus.Envelope.CloudEvents`) with 16+ named metrics and their exact string names
- Separate reference page ([docs.particular.net/monitoring/metrics/definitions](https://docs.particular.net/monitoring/metrics/definitions)) that deeply explains individual metrics -- notably "critical time" gets a multi-paragraph explanation including the clock-drift problem across machines, which is genuinely useful operational knowledge
- Points to concrete, runnable samples with real Jaeger and Prometheus/Grafana screenshots ([Jaeger sample](https://docs.particular.net/samples/open-telemetry/jaeger/), [Grafana sample](https://docs.particular.net/samples/open-telemetry/prometheus-grafana/))
- Documents "host identifier alignment" so that NServiceBus instance IDs match across NServiceBus, OpenTelemetry, and ServiceControl tooling -- a subtle but practical concern
- Covers trace continuity nuances: `SendOptions.StartNewTraceOnReceive()` vs default behavior, behavior with delayed retries

**Weaknesses:**

- Does not document span attribute keys in the main page (attributes are inferred, not listed)
- Metric stability warning is buried ("metric definitions are not yet finalized")
- Instrumentation opt-in is version-dependent (enabled by default in v10, opt-in in v8-9) which adds version-conditional complexity
- No troubleshooting section in the main OTel page

**Level of detail:** High on metrics, moderate on traces, absent on span attributes.

**Dashboard approach:** Links to real sample Grafana dashboards with export JSON, includes Jaeger UI screenshots.

---

#### MassTransit

**Source:** [masstransit.io/documentation/configuration/observability](https://masstransit.io/documentation/configuration/observability)

**Strengths:**

- Most complete metric catalog among all three competitors: 19 counters, 6 gauges, 7 histograms, 12 label dimensions -- all listed on a single page
- Covers non-OTel observer types (bus observers, receive-endpoint observers, pipeline observers for send/consume/publish, state machine observers) which gives a fuller mental model of what is monitorable
- Grafana community dashboard exists at [grafana.com/grafana/dashboards/17680](https://grafana.com/grafana/dashboards/17680-masstransit-messages-monitoring/) -- linked from community, not the official docs
- Includes Application Insights and Prometheus integration configurations

**Weaknesses:**

- No span attribute documentation at all
- No troubleshooting section
- No distributed tracing context-propagation explanation
- No dashboard screenshots in the main docs page itself
- No explanation of the "why" behind metric categories (sagas vs handlers vs activities)
- Histograms use milliseconds -- deviates from the OTel spec which requires seconds

**Level of detail:** Very high on metrics quantity, low on tracing quality, absent on conceptual explanation.

**Dashboard approach:** Implicitly references community Grafana dashboard but does not embed or link it from the main docs.

---

#### Wolverine

**Source:** [wolverinefx.net/guide/logging](https://wolverinefx.net/guide/logging)

**Strengths:**

- Uniquely strong on **selective telemetry control**: `TelemetryEnabled()` per-endpoint disabling, `[WolverineLogging]` per-message-type attribute, health-check trace filtering -- all practical noise-reduction tools
- Explicit span/activity name list (~15 named activities like `wolverine.envelope.discarded`, `wolverine.circuit.breaker.triggered`) -- good operational reference for alert rules
- Documents structured logging correlated to OTel spans (log entries carry span/trace IDs) -- a gap in Mocha's documentation
- Covers `[Audit]` attribute for including domain properties in structured logs -- practical operational feature not covered elsewhere
- Non-invasive setup story: two lines, no app code changes

**Weaknesses:**

- No metric attribute/dimension documentation
- No distributed trace propagation explanation (the page doesn't explain how context crosses service boundaries)
- No troubleshooting section
- No dashboard or visualization references

**Level of detail:** High on logging/structured-log correlation, moderate on metrics, low on trace propagation, absent on span attributes.

**Dashboard approach:** Mentions Jaeger and Honeycomb by name in blog posts, but the official docs show no dashboards.

---

### Summary Comparison Table

| Dimension                       | Mocha                          | NServiceBus           | MassTransit     | Wolverine |
| ------------------------------- | ------------------------------ | --------------------- | --------------- | --------- |
| Span attribute table            | Yes (11)                       | No                    | No              | No        |
| Metric catalog completeness     | Moderate (4+6)                 | High (16)             | Highest (32)    | Low (6)   |
| Metric attribute/dimension docs | Yes                            | Partial               | Yes             | No        |
| Trace propagation explained     | Yes (header table + diagram)   | Partial               | No              | No        |
| Troubleshooting section         | Yes (4 scenarios)              | No                    | No              | No        |
| Dashboard/screenshot references | No                             | Yes (Jaeger, Grafana) | Community only  | No        |
| Selective telemetry control     | No                             | No                    | No              | Yes       |
| Custom observer / extensibility | Yes (full example)             | No                    | Yes (observers) | No        |
| "Why" explanations              | Yes ("Why three middlewares?") | Partial               | No              | No        |
| Log-trace correlation           | No                             | Partial               | No              | Yes       |
| OTel semconv compliance note    | No                             | No                    | No              | No        |

---

### Best Practices Found

**1. Explain trace propagation with a diagram (Mocha does this well)**
NServiceBus and MassTransit skip this entirely. The Mermaid sequence diagram in Mocha's page is valuable and should be kept.

**2. Show what traces look like in a real backend**
NServiceBus wins here -- actual Jaeger screenshots of real spans. The Mocha page describes three spans but never shows what they look like in practice. Even a simple description of what a user sees in the Aspire dashboard or a link to a screenshot would close this gap.

**3. Document per-metric dimensionality separately from the metric itself**
NServiceBus has a separate definitions page; MassTransit uses a labels table. The Mocha metric dimensions table is correct but harder to read because it is separated from the metric definitions it describes. A combined table (metric + its dimensions inline) would reduce cognitive jumping.

**4. Note OTel semantic convention stability status**
The OTel messaging conventions are currently in "Development" status per [opentelemetry.io/docs/specs/semconv/messaging/](https://opentelemetry.io/docs/specs/semconv/messaging/). None of the competitors mention this. Mocha's span attribute table claims to follow OTel semantic conventions but some attributes (like `messaging.handler.name` and `messaging.instance.id`) are not in the official spec -- a brief note acknowledging this avoids credibility issues.

**5. The custom trace headers deserve explicit acknowledgment**
Looking at the actual source code in `/home/pascal/kot/graphql-platform/src/Mocha/src/Mocha/Headers/MessageHeaders.cs` and `/home/pascal/kot/graphql-platform/src/Mocha/src/Mocha/Observability/OpenTelemetry.cs`, Mocha propagates `trace-id`, `span-id`, `trace-state`, and `parent-id` as four separate custom headers, not the W3C-standard single `traceparent` header. The documentation table presents these as if they are standard. This is worth a note: either "these are Mocha-specific headers that encode the same fields as W3C traceparent" or a future improvement to adopt `traceparent` directly should be flagged. No competitor uses the same custom header scheme.

**6. Address high-cardinality and sampling concerns early**
Mocha's troubleshooting section has a "Too many spans" entry near the end. Wolverine's TelemetryEnabled per-endpoint approach and NServiceBus's sampling notes suggest this is a real user concern. Moving sampling guidance earlier (after the "Verify spans appear" section) would serve users before they hit the problem.

---

### External References

**OpenTelemetry Official:**

- [OpenTelemetry Messaging Spans Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/) -- authoritative source for required/recommended span attributes
- [OpenTelemetry Messaging Metrics Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/messaging/messaging-metrics/) -- authoritative source for metric names, types, and units
- [OpenTelemetry Context Propagation](https://opentelemetry.io/docs/concepts/context-propagation/) -- explains propagators, W3C TraceContext, baggage

**W3C Standards:**

- [W3C Trace Context specification](https://www.w3.org/TR/trace-context/) -- the traceparent/tracestate header format; relevant because Mocha uses custom headers instead

**Microsoft .NET:**

- [Distributed tracing concepts in .NET](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-concepts) -- explains Activity, ActivitySource, W3C ID format in .NET

**Architecture Patterns:**

- [Distributed Tracing pattern -- microservices.io](https://microservices.io/patterns/observability/distributed-tracing.html) -- Chris Richardson's canonical pattern description
- [OpenTelemetry in Decoupled Event-Driven Architectures (CNCF)](https://www.cncf.io/blog/2023/11/02/opentelemetry-in-decoupled-event-driven-architectures-solving-for-the-black-box-when-your-consuming-applications-are-constantly-changing/) -- practical challenges specific to event-driven systems (malformed topics, TTL expiry, ACL failures)

**Competitor Samples (high quality):**

- [NServiceBus Prometheus + Grafana sample](https://docs.particular.net/samples/open-telemetry/prometheus-grafana/) -- includes exportable Grafana dashboard JSON
- [NServiceBus Jaeger sample with screenshots](https://docs.particular.net/samples/open-telemetry/jaeger/) -- shows what distributed traces look like across endpoints
- [MassTransit community Grafana dashboard](https://grafana.com/grafana/dashboards/17680-masstransit-messages-monitoring/) -- practical dashboard for reference

**Blog posts (for tone/approach reference):**

- [Wolverine Embraces Observability -- Tim Deschryver](https://timdeschryver.dev/blog/wolverine-embraces-observability) -- good example of showing real screenshots from Honeycomb/Application Insights to demonstrate the value of the instrumentation

---

### Recommendations

**High priority -- accuracy:**

1. **Acknowledge the custom header format.** The trace propagation headers table documents `trace-id`, `span-id`, `trace-state`, `parent-id` as if they are standard, but the actual code (confirmed in `MessageHeaders.cs`) uses Mocha-custom headers, not the W3C `traceparent` single header. Add a note: "Mocha uses these Mocha-specific headers rather than the W3C `traceparent` format. If interop with non-Mocha producers is required, the receive middleware falls back to starting a new root trace." This is both accurate and clarifying.

2. **Flag non-standard span attributes.** The span attributes table includes `messaging.handler.name`, `messaging.instance.id`, and `messaging.message.type` which are not in the official OTel messaging semconv. The table says it follows OTel conventions. Either link to the exact spec version being targeted or add "(Mocha extension)" to custom attributes.

**Medium priority -- reducing bloat:**

3. **Collapse the Reference section.** The current reference section has seven separate subsections (ActivitySource, Span attributes, Metrics, Queue metrics, Topic metrics, Metric dimensions, Trace headers, IBusDiagnosticObserver interface, Registration method). The `IBusDiagnosticObserver` full interface listing (16 lines of C# with comments) adds length without being a usage guide -- it belongs in the API reference, not the conceptual observability page. Move it to a "See also" link or condense to just the method signatures in a compact table.

4. **Merge metric + dimension tables.** The current structure has Metrics, Queue metrics, and Topic metrics as three separate tables, then Metric dimensions as a fourth table. Users have to cross-reference to know which dimensions apply to which metric. Consider a combined table per metric group or inline dimension columns.

5. **Move the Aspire configuration block.** The Aspire integration section (25 lines of code) appears between trace propagation and custom observers -- both of which are conceptually heavier. Aspire is a specific integration scenario; moving it to the end before Troubleshooting (or to a collapsed aside) keeps the main conceptual flow intact.

**Low priority -- adding value:**

6. **Add one "what you will see" visual.** Even a short text description of what the three spans look like in the Aspire dashboard -- e.g., "In Aspire Dashboard, you will see a `publish rabbitmq://...` span from the publishing service, linked to a `receive ...` span in the consumer service, with a child `consumer OrderPlacedEventHandler` span" -- grounds the abstract span names in observable reality. Competitors with screenshots have a significant usability advantage here.

7. **Add a log-trace correlation note.** Wolverine documents that structured log entries include span/trace IDs for correlation. Mocha's page is silent on logging. A one-paragraph note on how `ILogger` structured logs automatically include `TraceId`/`SpanId` when OpenTelemetry is configured (which is true for any .NET app using `AddOpenTelemetry().WithLogging()`) would complete the "three pillars" story.

8. **Reference the OTel semconv stability caveat.** The messaging conventions are in Development status. Adding "Mocha follows the OpenTelemetry messaging semantic conventions (currently in development status at v1.x)" provides users important context if they are setting up alert rules or dashboards and should expect attribute name changes.
