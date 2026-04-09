# Competitor Documentation Research: .NET Messaging Framework Kafka/Transport Docs

## Executive Summary

This document analyzes how four competing .NET messaging frameworks document their transport layers, with emphasis on Kafka support. The goal is to identify best practices, common structures, and gaps that our Mocha Kafka transport documentation should address.

**Frameworks analyzed:**
1. **MassTransit** -- Mature Kafka support via "Rider" pattern
2. **Wolverine** -- Modern, convention-over-configuration approach with Kafka transport
3. **NServiceBus** -- Enterprise-grade, deliberately chose NOT to support Kafka natively
4. **Brighter (Paramore)** -- Pattern-first framework with Kafka gateway support

---

## 1. MassTransit -- Kafka Transport Documentation

### Documentation Structure
- **Two separate pages**: "Kafka Rider" (concepts/overview) and "Kafka Configuration" (detailed setup)
- Organized around the "Rider" abstraction (Kafka is not a first-class transport, but a rider)
- Uses tabular format for configuration options with defaults

### Topics Covered
- **Getting Started**: Basic rider setup with `AddRider` + `UsingKafka` pattern
- **Topic Endpoints**: Connecting consumers to topics with consumer groups
- **Wildcard/Regex Topics**: Pattern-based topic subscriptions
- **Checkpoint Settings**: `CheckpointInterval`, `CheckpointMessageCount`, `MessageLimit` with defaults table
- **Scalability Settings**: `ConcurrentConsumerLimit`, `ConcurrentDeliveryLimit`, `PrefetchCount` with defaults
- **Producer Configuration**: Registered producers and dynamic producers via `ITopicProducerProvider`
- **Tombstone Messages**: Null-payload deletion pattern
- **Multiple Message Types per Topic**: Schema registry approach (referenced, not deeply explained)

### Code Examples
- Basic rider configuration (consumer + host + topic endpoint)
- Regex topic subscriptions
- Producer registration and injection
- Dynamic producer creation
- Tombstone serializer implementation

### What's Missing / User Pain Points (from GitHub Issues/Discussions)
- **Topic creation responsibility** is unclear -- who creates topics, producer or consumer?
- **No built-in error/dead-letter topic** support (unlike RabbitMQ transport)
- **No transactional outbox** support with Kafka riders
- **Checkpointing behavior** is confusing; users don't understand when offsets move
- **Consumer rebalancing** issues during rolling deployments are undocumented
- **Monitoring/observability** guidance is absent
- **Health check** configuration is not documented
- No troubleshooting section
- No diagrams or visual aids

### Strengths
- Clear, concise configuration tables with defaults
- Good separation of concerns (overview vs. configuration)
- Practical code examples that are copy-pasteable

### Weaknesses
- Lacks conceptual explanation of HOW the rider maps to Kafka primitives
- No "getting started from scratch" tutorial
- No migration guide from raw Confluent client
- No operational guidance (monitoring, scaling, troubleshooting)

---

## 2. Wolverine -- Kafka Transport Documentation

### Documentation Structure
- **Single comprehensive page** covering all Kafka features
- Organized by capability: Publishing, Listening, Advanced Features
- Uses inline code samples extracted from test projects (DocumentationSamples.cs)
- Part of a broader transport docs section that covers 15+ transports with consistent structure

### Topics Covered
- **Installation**: NuGet package reference
- **Basic Setup**: `UseKafka(connectionString)` with client configuration callbacks
- **Publishing**: Automatic topic routing, explicit subscriptions, partition configuration
- **Listening**: Single/multi-topic listeners, consumer groups, processing modes (inline/buffered/durable)
- **Consumer Settings & Delivery Guarantees**: Auto-commit behavior in durable vs. non-durable mode
- **Partition Key Management**: `DeliveryOptions.PartitionKey`, GroupId propagation
- **Schema Registry**: JSON Schema and Avro serializer integration
- **Multi-Broker Support**: Named brokers for multiple Kafka clusters
- **Dead Letter Queues**: Native DLQ with `EnableNativeDeadLetterQueue()`, custom topic names
- **Interoperability**: Raw JSON send/receive for non-Wolverine systems
- **Tombstone Messages**: `KafkaTombstone` support
- **Resource Setup**: Auto-create topics on startup
- **Consume-Only Mode**: Disable sending for consumer-only services
- **Known Limitations**: Requeue not supported, becomes inline retry

### Code Examples
- Basic Kafka configuration
- Publishing with partition keys
- Listening with consumer groups
- Multi-topic listening
- Raw JSON interop
- Schema registry setup
- Dead letter queue configuration
- Multi-broker named configuration

### Strengths
- **Most comprehensive** of all frameworks analyzed
- Documents both happy path AND limitations explicitly
- Covers interoperability with non-framework systems
- Includes delivery guarantee semantics (durable vs. non-durable)
- Multi-broker support for complex architectures
- Dead letter queue support with metadata headers
- Native health check infrastructure (recently added)
- LLM-friendly documentation format available

### Weaknesses
- Single long page can be hard to navigate
- No conceptual diagrams
- No troubleshooting section
- Configuration options are inline rather than tabulated
- No performance tuning guidance
- No monitoring/observability guide (though health checks exist)

---

## 3. NServiceBus -- Transport Documentation

### Documentation Structure (Gold Standard for Organization)
- **Landing page** with transport overview and selection guidance
- **Per-transport sections** with consistent sub-pages:
  - Connection Settings
  - Routing Topology
  - Delayed Delivery
  - Native Integration
  - Transactions and Delivery Guarantees
  - Scripting/Operational Automation
- **Cross-cutting pages**:
  - Transport Selection Guide (comparison matrix)
  - Transport Transactions (4 modes explained with diagrams)
  - Creating Queues

### Topics Covered (across all transports)
- **Selecting a Transport**: Decision criteria (cloud vs. on-prem, interop, performance)
- **Transport Transactions**: 4 modes with consistency guarantees
  - Transaction Scope (distributed)
  - Sends Atomic with Receive
  - Receive Only
  - Unreliable
- **Atomicity, Consistency, Idempotency**: Conceptual explanations
- **Risk Scenarios**: Ghost messages, zombie records, partial updates
- **Outbox Pattern**: Exactly-once semantics without distributed transactions
- **Broker Requirements**: Version compatibility, required features
- **Routing Topology**: Queue/exchange/binding management
- **Native Integration**: Interop with non-NServiceBus endpoints
- **Scripting**: Deployment automation

### Visual Aids
- **Mermaid diagrams** showing message flow through transaction modes
- **Comparison tables** for transport selection
- **Architecture diagrams** for topology

### Kafka-Specific Position
NServiceBus deliberately does NOT support Kafka as a transport. Their blog post "Let's Talk About Kafka" explains:
- Kafka is a **partitioned log**, not a message queue -- fundamentally different abstractions
- Message queues "exist to become empty"; Kafka events persist for repeated reads
- Forcing Kafka to behave like a queue requires "problematic workarounds"
- Recommended pattern: Use Kafka for data ingestion, emit business events to a message queue

This is a valuable design perspective our docs should acknowledge.

### Strengths
- **Best documentation structure** of all frameworks
- Consistent per-transport layout (users know where to find things)
- Excellent conceptual documentation (transactions, delivery guarantees)
- Transport selection guide helps users make informed choices
- Diagrams for complex flows
- Clear versioning and upgrade guides
- Addresses operational concerns (scripting, deployment)

### Weaknesses
- No Kafka support (by design)
- Enterprise/commercial focus may not appeal to all audiences
- Dense -- can be overwhelming for beginners

---

## 4. Brighter (Paramore) -- Kafka Transport Documentation

### Documentation Structure
- Hosted on GitBook
- Organized by concern: Overview -> Configuration -> Advanced Topics
- Separate pages for basic concepts, basic configuration, and Kafka-specific configuration
- Pattern-first approach (Command, Query, Event patterns explained before transport details)

### Topics Covered
- **Connection Configuration**: `KafkaMessagingGatewayConnection` with all options
  - Bootstrap servers, security protocol, SASL mechanisms, SSL certificates, debug flags
- **Producer Configuration**: Detailed `KafkaPublication` settings
  - Replication/acks, batching, idempotence, linger, retries, timeouts, partitioner, transactional ID
  - Config hooks for Confluent client passthrough
- **Consumer Configuration**: Detailed `KafkaSubscription` settings
  - Commit batch size, group ID, isolation level, poll intervals, offset reset, session timeout
  - Partition assignment strategies (RoundRobin, Range, CooperativeSticky -- with note that Sticky is unsupported)
  - Config hooks with practical examples (fetch tuning, statistics, security, debugging)
- **Offset Management**: Deep dive into commit behavior
  - Core principles (manual commit after handler completion)
  - Flush behavior and concurrency
  - Sweep mechanism for low-throughput consumers
  - Rebalancing and shutdown offset handling
- **Schema Registry Integration**: Full setup with `CachedSchemaRegistryClient`, message mapper implementation
- **Error Handling**:
  - Blocking retry via Polly policies
  - Load shedding pattern
  - Non-blocking retry pattern (user-implemented)
  - Explicit note: "Brighter does not currently support requeue with delay for Kafka"
- **Topic Auto-Creation**: Recommendation to disable `auto.create.topics.enable`
- **Local vs. Production Configuration**: Separate examples for dev and prod environments

### Code Examples
- Local development connection setup
- Production connection with SASL/SSL
- Producer configuration with topic settings
- Consumer subscription with all options
- ConfigHook examples (fetch tuning, statistics, security, debugging)
- Schema registry message mapper (full implementation)
- Partition assignment strategy selection

### Strengths
- **Most detailed Kafka-specific configuration documentation** of all frameworks
- Excellent offset management explanation with flush/sweep mechanics
- ConfigHook examples for common scenarios (debugging, security, performance)
- Separate dev vs. production configuration examples
- Honest about limitations (no non-blocking retry, no CooperativeSticky)
- Schema registry integration with full working code

### Weaknesses
- Documentation is spread across GitBook which can have navigation issues (404 errors encountered)
- No diagrams or visual aids
- No getting-started tutorial
- No monitoring/observability guidance
- No health check documentation
- No troubleshooting section
- Pattern-heavy conceptual layer may confuse users who just want to configure Kafka

---

## Synthesis: Cross-Framework Analysis

### Topics Universally Covered (All 4 Frameworks)

| Topic | MassTransit | Wolverine | NServiceBus* | Brighter |
|-------|:-----------:|:---------:|:------------:|:--------:|
| Basic connection/host setup | Yes | Yes | Yes | Yes |
| Consumer/listener configuration | Yes | Yes | Yes | Yes |
| Producer/publisher configuration | Yes | Yes | Yes | Yes |
| Consumer groups | Yes | Yes | Yes | Yes |
| Serialization | Partial | Yes | Yes | Yes |
| Delivery guarantees | Partial | Yes | Yes | Yes |
| Partition key management | Implicit | Yes | N/A | Yes |
| Error handling / DLQ | No | Yes | Yes | Partial |
| Interoperability | No | Yes | Yes | Partial |
| Schema Registry | Referenced | Yes | N/A | Yes |

*NServiceBus evaluated for general transport docs structure, not Kafka-specific.

### Topics Only the Best Docs Cover

| Topic | Who Covers It | Why It Matters |
|-------|---------------|----------------|
| Transport selection guide | NServiceBus | Helps users understand when to use which transport |
| Transaction modes / consistency | NServiceBus | Critical for understanding delivery guarantees |
| Dead letter queues | Wolverine | Essential for production error handling |
| Multi-broker support | Wolverine | Real-world architectures use multiple clusters |
| Offset management deep dive | Brighter | Key Kafka concept users struggle with |
| Dev vs. production config | Brighter | Practical for real adoption |
| Config hooks / passthrough | Brighter, Wolverine | Escape hatch for advanced Confluent settings |
| Known limitations | Wolverine, Brighter | Prevents user frustration |
| Raw JSON interop | Wolverine | Critical for brownfield integration |
| Tombstone messages | MassTransit, Wolverine | Log compaction pattern |
| Regex/wildcard topics | MassTransit | Dynamic topic subscription |
| Diagrams / visual aids | NServiceBus | Aids comprehension of complex flows |
| Troubleshooting | None well | Universal gap across all frameworks |
| Monitoring / observability | None well | Universal gap across all frameworks |
| Health checks | Wolverine (recent) | Production readiness requirement |
| Performance tuning | None well | Universal gap, critical for framework code |

### What's Missing Across ALL Frameworks

1. **Troubleshooting guides** -- No framework has a dedicated troubleshooting section for Kafka
2. **Monitoring/observability** -- No framework documents how to monitor their Kafka integration (metrics, dashboards, alerting)
3. **Performance tuning** -- No framework provides benchmarks or tuning guidance specific to their abstraction
4. **Migration guides** -- No framework shows how to migrate from raw Confluent client
5. **Architecture decision records** -- Why certain design choices were made
6. **Deployment patterns** -- Rolling updates, blue-green, canary with Kafka consumers
7. **Multi-tenancy** -- How to handle tenant isolation with Kafka topics
8. **Testing guidance** -- How to write tests against the transport (unit, integration, contract)

---

## Recommended Structure for Mocha Kafka Transport Docs

Based on this research, the optimal documentation structure combines NServiceBus's organizational rigor with Wolverine's comprehensiveness and Brighter's Kafka-specific depth:

### Tier 1: Getting Started (Must Have)
1. **Overview** -- What is the Mocha Kafka transport? Architecture diagram showing how it fits into Mocha's message bus
2. **Quick Start** -- Minimal working example (producer + consumer + Aspire)
3. **Installation** -- NuGet packages, prerequisites, supported .NET versions

### Tier 2: Configuration Reference (Must Have)
4. **Connection Configuration** -- Bootstrap servers, security (SASL/SSL), dev vs. production examples
5. **Producer Configuration** -- Topics, serialization, partition keys, batching, delivery options
6. **Consumer Configuration** -- Consumer groups, offset management, concurrency, processing modes
7. **Topology Management** -- Topic creation strategy (auto-create vs. pre-provisioned)

### Tier 3: Concepts & Patterns (Should Have)
8. **Delivery Guarantees** -- At-least-once, exactly-once semantics, idempotency patterns
9. **Error Handling** -- Dead letter topics, retry policies, poison message handling
10. **Serialization** -- Built-in serialization, Schema Registry integration, custom serializers
11. **Interoperability** -- Working with non-Mocha producers/consumers, raw message handling

### Tier 4: Production Readiness (Should Have)
12. **Monitoring & Observability** -- OpenTelemetry integration, metrics, consumer lag tracking
13. **Health Checks** -- Kafka health check configuration for ASP.NET Core
14. **Scaling** -- Partition-based scaling, consumer group rebalancing, concurrency tuning
15. **Deployment** -- Rolling updates, graceful shutdown, rebalancing during deploys

### Tier 5: Advanced Topics (Nice to Have)
16. **Multi-Broker Support** -- Connecting to multiple Kafka clusters
17. **Tombstone Messages** -- Log compaction support
18. **Testing** -- Unit testing with Mocha's test utilities, integration testing with Testcontainers
19. **Troubleshooting** -- Common issues, debugging tips, FAQ

### Tier 6: Reference (Nice to Have)
20. **Configuration Reference** -- Complete table of all options with types, defaults, and descriptions
21. **API Reference** -- Key interfaces and classes
22. **Samples** -- Links to example projects (like the KafkaTransport example in the repo)

---

## Key Differentiators We Should Aim For

Based on gaps identified across all competitors:

1. **Aspire-first experience** -- No competitor shows Kafka transport with .NET Aspire orchestration. Our example project already uses Aspire; docs should lean into this.

2. **Visual architecture diagrams** -- Show message flow from producer through Mocha to Kafka and back to consumer. NServiceBus is the only one with diagrams; this is a clear differentiator.

3. **Troubleshooting section** -- Every framework is missing this. Include common error messages, debugging steps, and FAQ.

4. **Monitoring integration** -- Document OpenTelemetry traces/metrics out of the box. No competitor does this well.

5. **Testing guidance** -- Show how to test code that uses the transport. No competitor covers this adequately.

6. **Honest limitations** -- Following Wolverine and Brighter's lead, explicitly document what the transport does NOT support. This builds trust.

7. **Dev vs. production parity** -- Show both local development (with Docker/Aspire) and production configurations side by side, as Brighter does.

---

## Sources

### MassTransit
- [Kafka Rider](https://masstransit.io/documentation/transports/kafka)
- [Kafka Configuration](https://masstransit.io/documentation/configuration/transports/kafka)
- [GitHub Discussions - Topic Partitioning](https://github.com/MassTransit/MassTransit/discussions/3424)
- [GitHub Discussions - Scale Consumers](https://github.com/MassTransit/MassTransit/discussions/3441)
- [GitHub Discussions - Bunch of Questions](https://github.com/MassTransit/MassTransit/discussions/4247)

### Wolverine
- [Using Kafka](https://wolverinefx.net/guide/messaging/transports/kafka)
- [Getting Started with Messaging](https://wolverinefx.net/guide/messaging/introduction.html)
- [Wolverine GitHub](https://github.com/JasperFx/wolverine)

### NServiceBus
- [Transports Overview](https://docs.particular.net/transports/)
- [Selecting a Transport](https://docs.particular.net/transports/selecting)
- [Transport Transactions](https://docs.particular.net/transports/transactions)
- [RabbitMQ Transport](https://docs.particular.net/transports/rabbitmq/)
- [Let's Talk About Kafka (Blog)](https://particular.net/blog/lets-talk-about-kafka)

### Brighter (Paramore)
- [Kafka Configuration](https://brightercommand.gitbook.io/paramore-brighter-documentation/guaranteed-at-least-once/kafkaconfiguration.md)
- [Basic Concepts](https://brightercommand.gitbook.io/paramore-brighter-documentation/overview/basicconcepts)
- [Basic Configuration](https://brightercommand.gitbook.io/paramore-brighter-documentation/brighter-configuration/brighterbasicconfiguration)
- [Brighter GitHub](https://github.com/BrighterCommand/Brighter)

### General Kafka/.NET
- [Confluent .NET Client](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)
- [Kafka Monitoring Best Practices](https://github.com/AutoMQ/automq/wiki/Kafka-Monitoring:-Tools-&-Best-Practices)
- [Kafka Health Checks](https://www.pagerduty.com/eng/kafka-health-checks/)
