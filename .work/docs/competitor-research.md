# Competitor Documentation Research: Azure Event Hub Transports

## 1. MassTransit Event Hub Rider

**Sources**: [MassTransit Riders Concepts](https://masstransit.io/documentation/concepts/riders), [MassTransit Event Hub Config (v7)](https://masstransit-v7.netlify.app/usage/riders/eventhub.html), [MassTransit Event Hub Config](https://masstransit-project.com/usage/riders/eventhub)

### Documentation Structure

MassTransit documents Event Hub as a "Rider" — a separate integration pattern alongside the primary bus transport. Their docs are split into:

1. **Concepts page** — Explains what riders are and why they exist separately from the bus transport. Key insight: "many traditional messaging concepts and idioms do not apply directly to streaming platforms."
2. **Configuration page** — Practical setup with code examples for consuming and producing.

### Sections Covered

- **Overview**: Brief description ("Azure Event Hub is included as a Rider, and supports consuming and producing messages from/to Azure event hubs")
- **NuGet packages**: Lists required packages (`MassTransit.EventHub`, etc.)
- **Consumer configuration**: Full code example showing `AddRider()`, `UsingEventHub()`, `ReceiveEndpoint()` with consumer registration
- **Producer configuration**: Code example showing `IEventHubProducerProvider` and `GetProducer()` / `Produce<T>()`
- **Consumer groups**: Brief note that "consumer group specified should be unique to the application, and shared by a cluster of service instances for load balancing"
- **Checkpointing**: Notes that "Rider implementation is taking full responsibility of Checkpointing, there is no ability to change it"

### What They Explain vs Assume

- **Assume**: Reader knows Azure Event Hub concepts (partitions, consumer groups, offsets)
- **Assume**: Reader knows MassTransit fundamentals (consumers, sagas, receive endpoints)
- **Explain**: How riders differ from bus transports, the lack of pub-sub semantics in Event Hub
- **Explain**: That consumers/sagas can be configured but there's no implicit message type binding

### Key Limitations in Their Docs

- **No diagrams** — Purely text and code
- **No troubleshooting/FAQ** — Nothing about common issues
- **No getting started flow** — Jumps straight into configuration code
- **Very thin** — The entire Event Hub rider documentation is perhaps 2-3 pages of content
- **No performance guidance** — No discussion of throughput, batching, or optimization
- **No explanation of checkpointing internals** — Just says "we handle it"

### What They Do Well

- Clear distinction that Event Hub is NOT a message broker (no pub-sub)
- Explicit about consumer group uniqueness requirement
- Code examples follow their established patterns (consistent with Kafka rider docs)
- Honest about limitations (checkpointing is opaque)

---

## 2. NServiceBus

**Sources**: [NServiceBus Transports](https://docs.particular.net/transports/), [NServiceBus Azure](https://docs.particular.net/nservicebus/azure/), [GitHub Issue #204](https://github.com/Particular/NServiceBus.Azure/issues/204), [GitHub Issue #12](https://github.com/Particular/NServiceBus.AzureServiceBus/issues/12)

### Event Hub Support

**NServiceBus does NOT support Azure Event Hubs as a transport.** There are open GitHub issues requesting it (#204, #12), but the team has not implemented it. Their reasoning (from the issues) is that Event Hubs is a data streaming platform, not a traditional message queue, and the semantics don't align well with NServiceBus's messaging model.

### Azure Service Bus Transport Documentation Pattern (for reference)

NServiceBus's Azure Service Bus transport docs follow a consistent, well-structured pattern that all their transports share:

1. **Overview with "Transport at a glance" table** — Quick reference covering features (transactions, pub/sub, timeouts, deployment capabilities)
2. **Configuration** — Connection settings, endpoint setup
3. **Topology** — How queues and topics are organized
4. **Native integration** — How to interop with non-NServiceBus systems
5. **Operational scripting** — Queue/topic creation scripts
6. **Transaction support** — Detailed guidance
7. **Troubleshooting** — Dedicated section
8. **Upgrade guides** — Version migration documentation
9. **Samples** — Send/reply, pub/sub, native integration examples

### What NServiceBus Does Well (general transport doc pattern)

- **Consistent structure** across all transports — users know where to find information
- **"Transport at a glance" table** — Instant feature comparison
- **Operational scripting** — Real-world deployment concerns
- **Topology documentation** — Visual understanding of how messages flow
- **Upgrade guides** — Shows maturity and care for existing users
- **Layered approach**: conceptual overview -> configuration -> specialized features -> samples -> related resources

---

## 3. Azure SDK Official Documentation

**Sources**: [Azure.Messaging.EventHubs README](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs/README.md), [Azure.Messaging.EventHubs.Processor README](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs.Processor/README.md)

### Event Hubs Client Library (.NET) Structure

1. **Getting Started**
   - Prerequisites (Azure subscription, Event Hub namespace)
   - Install the package (NuGet)
   - Authenticate the client (connection string or Azure AD)
2. **Key Concepts**
   - Client lifetime and thread safety
   - Event Hub clients (different types for different purposes)
   - Producers and consumers
   - Partitions and consumer groups
3. **Examples**
   - Inspect an Event Hub
   - Publish events to an Event Hub (batched)
   - Read events from an Event Hub
   - Read events from a specific partition
   - Use EventProcessorClient with Azure Storage
   - Use Azure Active Directory credentials
4. **Troubleshooting** — References separate troubleshooting guide and migration docs
5. **Next Steps** — Links to samples, API reference
6. **Contributing**

### Event Hubs Processor Library (.NET) Structure

1. **Getting Started**
   - Prerequisites (Azure subscription, Event Hubs namespace, **Storage account with container**)
   - Install the package
   - Obtain connection credentials
2. **Key Concepts**
   - **Checkpointing**: "a process by which readers mark and persist their position for events that have been processed for a partition"
   - **Partition management**: Each partition is an ordered sequence
   - **Load balancing**: Processors "distribute and share the responsibility in the context of a consumer group"
   - **Duplicate events**: Explicitly warns that "multiple readers on the same partition may receive duplicate events"
3. **Examples**
   - Create BlobContainerClient for checkpoint storage
   - Create EventProcessorClient
   - Configure event and error handlers
   - Start/stop processing with cleanup
4. **Troubleshooting**

### What Azure SDK Does Well

- **Progressive disclosure** — Simple concepts first, advanced later
- **Explicit prerequisites** — Including storage account for checkpointing
- **Thread safety documentation** — Critical for real-world usage
- **Warning about duplicates** — Sets correct expectations
- **Separate libraries** — Simple consumer vs processor client, clear when to use each

---

## 4. Azure Event Hubs Concepts (Microsoft Learn)

**Source**: [Event Hubs features and terminology](https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-features), [Balance partition load](https://learn.microsoft.com/en-us/azure/event-hubs/event-processor-balance-partition-load)

### Key Concepts Documented

| Concept | Description |
|---------|-------------|
| **Namespace** | Management container for one or more event hubs. Controls network access and scaling. |
| **Event hub** | An append-only log that stores events. Equivalent to a Kafka topic. |
| **Partition** | Ordered sequence of events within an event hub. Enables parallel processing. |
| **Producer/Publisher** | Application that sends events to an event hub. |
| **Consumer** | Application that reads events from an event hub. |
| **Consumer group** | Independent view of the event stream. Multiple groups can read the same data separately. |
| **Offset** | Position of an event within a partition. Used to track reading progress. |
| **Checkpointing** | Saving the current offset so consumers can resume from where they left off. |

### Diagrams/Visualizations Used

Microsoft uses several diagrams in their docs:
- **Namespace diagram** — Shows namespace containing multiple event hubs
- **Partitions diagram** — Shows event hub with multiple partitions, events flowing in
- **Event sequence diagram** — Shows older-to-newer sequence within a partition
- **Partition keys diagram** — Shows how keys map events to specific partitions
- **Consumer groups diagram** — Shows multiple consumer groups reading from the same hub
- **Offset diagram** — Shows events in a partition with offset positions
- **Capture diagram** — Shows data flowing to Azure Storage

### Partitions — Deep Dive

- Ordered sequence of events, append-only (like a commit log)
- Cannot change partition count after creation (standard tier)
- Partition count = max parallel consumers per consumer group
- Per-partition throughput: ~1 MB/s ingress, ~2 MB/s egress (standard tier)
- Partition key hashing determines which partition receives events
- Round-robin if no partition key specified

### Consumer Groups — Deep Dive

- Independent view of the event stream
- Multiple groups can read simultaneously at their own pace
- Recommended: one active reader per partition per consumer group
- Default group: `$Default` exists on every event hub
- Best practice: separate consumer groups for each application

### Checkpointing — Deep Dive

- Consumer's responsibility (not the service)
- Occurs per-partition within a consumer group
- Enables resumption, failover, and replay
- Azure Blob Storage recommended as checkpoint store
- Best practices: separate container per consumer group, same region as application
- Frequency tradeoff: every event = costly writes; batch = potential reprocessing

### Load Balancing — Deep Dive

- EventProcessorClient handles partition ownership automatically
- Ownership tracked via checkpoint store (blob storage)
- Processors communicate via checkpoint store to balance load
- When instances join/leave, partitions are redistributed
- Thread safety: events from same partition processed sequentially; different partitions can be concurrent

### Protocols Supported

| Protocol | Send | Receive | Best for |
|----------|------|---------|----------|
| AMQP 1.0 | Yes | Yes | High throughput, low latency |
| Apache Kafka | Yes | Yes | Existing Kafka applications |
| HTTPS | Yes | No | Lightweight clients |

### Event Retention

| Tier | Default | Maximum |
|------|---------|---------|
| Standard | 1 hour | 7 days |
| Premium | 1 hour | 90 days |
| Dedicated | 1 hour | 90 days |

### Access Control

- Microsoft Entra ID (OAuth 2.0) with RBAC roles
- Shared Access Signatures (SAS) for scoped access
- Built-in roles: Data Owner, Data Sender, Data Receiver

---

## 5. Recommendations for Our Documentation

### Must-Have Sections (based on competitor analysis)

1. **Overview** — What the transport is, when to use it, how it fits into Mocha
2. **Key Concepts** — Brief explanation of Event Hub concepts users need (partitions, consumer groups, checkpointing) — don't assume the reader is an Event Hub expert
3. **Getting Started / Quick Start** — Step-by-step from zero to working transport
   - Prerequisites (Azure resources needed)
   - Package installation
   - Minimal configuration code
4. **Configuration Reference** — All options with descriptions and defaults
5. **Producing Events** — How to send events, partition key strategies
6. **Consuming Events** — How to receive and process events
7. **Checkpointing** — How our transport handles checkpointing (MassTransit is vague here — we should be better)
8. **Consumer Groups & Scaling** — How to scale out, partition ownership
9. **Error Handling** — What happens on failures, retry behavior

### Should-Have Sections

10. **Architecture Diagram** — Visual showing how the transport connects Mocha to Event Hubs (Microsoft does this well)
11. **Partition Strategy** — Guidance on choosing partition count, partition keys
12. **Performance Considerations** — Throughput limits, batching, optimization tips
13. **Troubleshooting / FAQ** — Common issues and solutions (nobody does this well — opportunity to differentiate)

### Documentation Quality Bar

- **Be more thorough than MassTransit** — Their docs are thin, code-only, no concepts
- **Follow NServiceBus's consistent structure** — Transport-at-a-glance table, layered approach
- **Match Azure SDK's progressive disclosure** — Simple first, then advanced
- **Include diagrams** like Microsoft Learn — Visual understanding matters
- **Explain what we abstract** — If we handle checkpointing automatically, explain what that means and what tradeoffs it implies (MassTransit just says "we handle it" which is frustrating)
- **Set correct expectations** — Event Hubs is NOT a message queue. Explain at-least-once delivery, potential duplicates, ordering guarantees per partition only

### Key Differentiators We Should Emphasize

1. **How Mocha's transport abstraction maps to Event Hub concepts** — This is unique to us
2. **Checkpointing behavior** — Be transparent about what we automate and what users control
3. **Integration with the broader Mocha ecosystem** — How Event Hub transport works alongside other transports
4. **Practical guidance** — Partition count recommendations, consumer group strategies, error handling patterns
