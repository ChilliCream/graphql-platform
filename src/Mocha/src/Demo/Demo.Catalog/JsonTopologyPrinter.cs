using System.Text.Json;
using System.Text.Json.Serialization;
using Mocha;
using Mocha.Sagas;
using Mocha.Transport.InMemory;
using Mocha.Transport.RabbitMQ;

namespace Explorer;

/// <summary>
/// Exports message bus topology as a comprehensive JSON document.
/// Uses natural identifiers (URIs, identities, names) from the system rather than synthetic IDs.
///
/// The topology is modeled as a transport-neutral graph:
/// - Entities represent passive messaging resources (queue, exchange, topic, subscription, consumerGroup)
/// - Links represent relationships or data flow between entities (bind, subscribe, forward)
///
/// This separation supports RabbitMQ, Kafka, Service Bus, in-memory, and future transports with one stable schema.
/// Transport adapters interpret entities and links per their specific semantics.
/// </summary>
public class JsonTopologyPrinter
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly List<ITransportTopologyExporter> _transportExporters = [];

    public JsonTopologyPrinter()
    {
        // Register default transport exporters
        RegisterTransportExporter(new RabbitMqTopologyExporter());
        RegisterTransportExporter(new InMemoryTopologyExporter());
    }

    /// <summary>
    /// Register a custom transport topology exporter for extensibility.
    /// </summary>
    public void RegisterTransportExporter(ITransportTopologyExporter exporter)
    {
        _transportExporters.Add(exporter);
    }

    public string PrintTopology(MessagingRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        var model = BuildTopologyModel(runtime);
        return JsonSerializer.Serialize(model, s_jsonOptions);
    }

    public JsonBusModel BuildTopologyModel(MessagingRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        var model = new JsonBusModel
        {
            Services =
            [
                new JsonServiceModel
                {
                    Host = ExportHostInfo(runtime),
                    MessageTypes = ExportMessageTypes(runtime),
                    Consumers = ExportConsumers(runtime),
                    Routes = new JsonRoutesModel
                    {
                        Inbound = ExportInboundRoutes(runtime),
                        Outbound = ExportOutboundRoutes(runtime)
                    },
                    Sagas = ExportSagas(runtime)
                }
            ],
            Transports = ExportTransports(runtime)
        };

        return model;
    }

    private static JsonHostModel ExportHostInfo(MessagingRuntime runtime)
    {
        return new JsonHostModel
        {
            ServiceName = runtime.Host.ServiceName,
            AssemblyName = runtime.Host.AssemblyName,
            InstanceId = runtime.Host.InstanceId.ToString("D")
        };
    }

    private static List<JsonMessageTypeModel> ExportMessageTypes(MessagingRuntime runtime)
    {
        var result = new List<JsonMessageTypeModel>();

        foreach (var messageType in runtime.Messages.MessageTypes)
        {
            result.Add(
                new JsonMessageTypeModel
                {
                    Identity = messageType.Identity,
                    RuntimeType = GetTypeName(messageType.RuntimeType),
                    RuntimeTypeFullName = messageType.RuntimeType.FullName,
                    IsInterface = messageType.IsInterface,
                    IsInternal = messageType.IsInternal,
                    DefaultContentType = messageType.DefaultContentType?.ToString(),
                    EnclosedMessageIdentities = messageType.EnclosedMessageIdentities.IsEmpty
                        ? null
                        : [.. messageType.EnclosedMessageIdentities]
                });
        }

        return result;
    }

    private static List<JsonConsumerModel> ExportConsumers(MessagingRuntime runtime)
    {
        var result = new List<JsonConsumerModel>();

        foreach (var consumer in runtime.Consumers)
        {
            string? sagaName = null;
            if (consumer is SagaConsumer sagaConsumer)
            {
                var saga = GetSagaFromConsumer(sagaConsumer);
                sagaName = saga?.Name;
            }

            result.Add(
                new JsonConsumerModel
                {
                    Name = consumer.Name,
                    IdentityType = GetTypeName(consumer.Identity),
                    IdentityTypeFullName = consumer.Identity.FullName,
                    SagaName = sagaName
                });
        }

        return result;
    }

    private static List<JsonInboundRouteModel> ExportInboundRoutes(MessagingRuntime runtime)
    {
        var result = new List<JsonInboundRouteModel>();

        foreach (var route in runtime.Router.InboundRoutes)
        {
            result.Add(
                new JsonInboundRouteModel
                {
                    Kind = route.Kind,
                    MessageTypeIdentity = route.MessageType?.Identity,
                    ConsumerName = route.Consumer?.Name,
                    Endpoint = route.Endpoint is not null
                        ? new JsonEndpointReference
                        {
                            Name = route.Endpoint.Name,
                            Address = route.Endpoint.Address?.ToString(),
                            TransportName = route.Endpoint.Transport.Name
                        }
                        : null
                });
        }

        return result;
    }

    private static List<JsonOutboundRouteModel> ExportOutboundRoutes(MessagingRuntime runtime)
    {
        var result = new List<JsonOutboundRouteModel>();

        foreach (var route in runtime.Router.OutboundRoutes)
        {
            result.Add(
                new JsonOutboundRouteModel
                {
                    Kind = route.Kind,
                    MessageTypeIdentity = route.MessageType.Identity,
                    Destination = route.Destination?.ToString(),
                    Endpoint = route.Endpoint is not null
                        ? new JsonEndpointReference
                        {
                            Name = route.Endpoint.Name,
                            Address = route.Endpoint.Address?.ToString(),
                            TransportName = route.Endpoint.Transport.Name
                        }
                        : null
                });
        }

        return result;
    }

    private List<JsonTransportModel> ExportTransports(MessagingRuntime runtime)
    {
        var result = new List<JsonTransportModel>();

        foreach (var transport in runtime.Transports)
        {
            var transportModel = new JsonTransportModel
            {
                // The topology address serves as the globally unique, stable identifier
                // - RabbitMQ: "rabbitmq://dev.rabbitmq.mycompany.com:5672/" (connection host)
                // - InMemory: "inmemory://MyService/" (assembly/service name)
                Identifier = transport.Topology.Address.ToString(),
                Name = transport.Name,
                Schema = transport.Schema,
                TransportType = transport.GetType().Name,
                ReceiveEndpoints = ExportReceiveEndpoints(transport),
                DispatchEndpoints = ExportDispatchEndpoints(transport),
                Topology = ExportTopology(transport)
            };

            result.Add(transportModel);
        }

        return result;
    }

    private static List<JsonReceiveEndpointModel> ExportReceiveEndpoints(MessagingTransport transport)
    {
        var result = new List<JsonReceiveEndpointModel>();

        foreach (var endpoint in transport.ReceiveEndpoints)
        {
            result.Add(
                new JsonReceiveEndpointModel
                {
                    Name = endpoint.Name,
                    Kind = endpoint.Kind,
                    Address = endpoint.Address?.ToString(),
                    Source = endpoint.Source is not null
                        ? new JsonTopologyResourceReference { Address = endpoint.Source.Address?.ToString() }
                        : null
                });
        }

        return result;
    }

    private static List<JsonDispatchEndpointModel> ExportDispatchEndpoints(MessagingTransport transport)
    {
        var result = new List<JsonDispatchEndpointModel>();

        foreach (var endpoint in transport.DispatchEndpoints)
        {
            result.Add(
                new JsonDispatchEndpointModel
                {
                    Name = endpoint.Name,
                    Kind = endpoint.Kind,
                    Address = endpoint.Address?.ToString(),
                    Destination = endpoint.Destination is not null
                        ? new JsonTopologyResourceReference { Address = endpoint.Destination.Address?.ToString() }
                        : null
                });
        }

        return result;
    }

    private JsonTopologyModel? ExportTopology(MessagingTransport transport)
    {
        // Find matching exporter
        foreach (var exporter in _transportExporters)
        {
            if (exporter.CanExport(transport))
            {
                return exporter.Export(transport);
            }
        }

        // Fallback: generic export
        return ExportGenericTopology(transport);
    }

    private static JsonTopologyModel ExportGenericTopology(MessagingTransport transport)
    {
        var entities = new List<JsonTopologyEntity>();

        // Track resources by direction
        var outboundResources = new HashSet<TopologyResource>();
        var inboundResources = new HashSet<TopologyResource>();

        // Receive endpoints connect to outbound resources (messages flow OUT from transport)
        foreach (var endpoint in transport.ReceiveEndpoints)
        {
            if (endpoint.Source is not null)
            {
                outboundResources.Add(endpoint.Source);
            }
        }

        // Dispatch endpoints connect to inbound resources (messages flow INTO transport)
        foreach (var endpoint in transport.DispatchEndpoints)
        {
            if (endpoint.Destination is not null)
            {
                inboundResources.Add(endpoint.Destination);
            }
        }

        foreach (var resource in outboundResources)
        {
            entities.Add(
                new JsonTopologyEntity
                {
                    Kind = resource.GetType().Name.ToLowerInvariant(),
                    Address = resource.Address?.ToString(),
                    Flow = "outbound"
                });
        }

        foreach (var resource in inboundResources)
        {
            // Skip if already added as outbound (resource used for both directions)
            if (outboundResources.Contains(resource))
            {
                continue;
            }

            entities.Add(
                new JsonTopologyEntity
                {
                    Kind = resource.GetType().Name.ToLowerInvariant(),
                    Address = resource.Address?.ToString(),
                    Flow = "inbound"
                });
        }

        return new JsonTopologyModel
        {
            Address = transport.Topology.Address.ToString(),
            Entities = entities,
            Links = []
        };
    }

    private static List<JsonSagaModel>? ExportSagas(MessagingRuntime runtime)
    {
        var sagas = new List<JsonSagaModel>();

        foreach (var consumer in runtime.Consumers)
        {
            if (consumer is not SagaConsumer sagaConsumer)
            {
                continue;
            }

            // Access the saga through reflection since SagaConsumer uses a primary constructor
            var saga = GetSagaFromConsumer(sagaConsumer);
            if (saga is null)
            {
                continue;
            }

            var sagaModel = new JsonSagaModel
            {
                Name = saga.Name,
                StateType = GetTypeName(saga.StateType),
                StateTypeFullName = saga.StateType.FullName,
                ConsumerName = consumer.Name,
                States = ExportSagaStates(saga)
            };

            sagas.Add(sagaModel);
        }

        return sagas.Count > 0 ? sagas : null;
    }

    private static Saga? GetSagaFromConsumer(SagaConsumer consumer)
    {
        // Primary constructor parameters are captured as fields with compiler-generated names
        // We look for any field of type Saga
        var fields = typeof(SagaConsumer).GetFields(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (typeof(Saga).IsAssignableFrom(field.FieldType))
            {
                return field.GetValue(consumer) as Saga;
            }
        }

        return null;
    }

    private static List<JsonSagaStateModel> ExportSagaStates(Saga saga)
    {
        var result = new List<JsonSagaStateModel>();

        foreach (var (stateName, state) in saga.States)
        {
            var stateModel = new JsonSagaStateModel
            {
                Name = stateName,
                IsInitial = state.IsInitial,
                IsFinal = state.IsFinal,
                OnEntry = state.OnEntry is not null ? ExportSagaLifeCycle(state.OnEntry) : null,
                Response = state.Response is not null
                    ? new JsonSagaResponseModel
                    {
                        EventType = GetTypeName(state.Response.EventType),
                        EventTypeFullName = state.Response.EventType.FullName
                    }
                    : null,
                Transitions = ExportSagaTransitions(state)
            };

            result.Add(stateModel);
        }

        return result;
    }

    private static JsonSagaLifeCycleModel ExportSagaLifeCycle(SagaLifeCycle lifeCycle)
    {
        return new JsonSagaLifeCycleModel
        {
            Publish = lifeCycle.Publish.IsEmpty
                ? null
                : lifeCycle
                    .Publish.Select(p => new JsonSagaEventModel
                    {
                        MessageType = GetTypeName(p.MessageType),
                        MessageTypeFullName = p.MessageType.FullName
                    })
                    .ToList(),
            Send = lifeCycle.Send.IsEmpty
                ? null
                : lifeCycle
                    .Send.Select(s => new JsonSagaEventModel
                    {
                        MessageType = GetTypeName(s.MessageType),
                        MessageTypeFullName = s.MessageType.FullName
                    })
                    .ToList()
        };
    }

    private static List<JsonSagaTransitionModel> ExportSagaTransitions(SagaState state)
    {
        var result = new List<JsonSagaTransitionModel>();

        foreach (var (eventType, transition) in state.Transitions)
        {
            var transitionModel = new JsonSagaTransitionModel
            {
                EventType = GetTypeName(eventType),
                EventTypeFullName = eventType.FullName,
                TransitionTo = transition.TransitionTo,
                TransitionKind = transition.TransitionKind,
                AutoProvision = transition.AutoProvision,
                Publish = transition.Publish.IsEmpty
                    ? null
                    : transition
                        .Publish.Select(p => new JsonSagaEventModel
                        {
                            MessageType = GetTypeName(p.MessageType),
                            MessageTypeFullName = p.MessageType.FullName
                        })
                        .ToList(),
                Send = transition.Send.IsEmpty
                    ? null
                    : transition
                        .Send.Select(s => new JsonSagaEventModel
                        {
                            MessageType = GetTypeName(s.MessageType),
                            MessageTypeFullName = s.MessageType.FullName
                        })
                        .ToList()
            };

            result.Add(transitionModel);
        }

        return result;
    }

    private static string GetTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var genericTypeName = type.Name.Split('`')[0];
        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetTypeName));
        return $"{genericTypeName}<{genericArgs}>";
    }
}

/// <summary>
/// Interface for transport-specific topology exporters.
/// Implement this interface to add support for new transport types.
///
/// Exporters map transport-specific resources to the generic entity/link model:
/// - Entities: passive messaging resources (queue, exchange, topic, subscription, consumerGroup)
/// - Links: relationships or data flow between entities (bind, subscribe, forward)
/// </summary>
public interface ITransportTopologyExporter
{
    /// <summary>
    /// Determines if this exporter can handle the given transport.
    /// </summary>
    bool CanExport(MessagingTransport transport);

    /// <summary>
    /// Exports the transport's topology to a transport-neutral graph model.
    /// </summary>
    JsonTopologyModel Export(MessagingTransport transport);
}

/// <summary>
/// RabbitMQ topology exporter.
/// Maps RabbitMQ resources to the generic entity/link model:
/// - Exchange → Entity (kind: "exchange")
/// - Queue → Entity (kind: "queue")
/// - Binding → Link (kind: "bind")
/// </summary>
public class RabbitMqTopologyExporter : ITransportTopologyExporter
{
    public bool CanExport(MessagingTransport transport) => transport is RabbitMQMessagingTransport;

    public JsonTopologyModel Export(MessagingTransport transport)
    {
        var rabbitTransport = (RabbitMQMessagingTransport)transport;
        var topology = (RabbitMQMessagingTopology)rabbitTransport.Topology;

        var entities = new List<JsonTopologyEntity>();
        var links = new List<JsonTopologyLink>();

        // Export exchanges as entities (inbound - dispatch endpoints connect to them)
        foreach (var exchange in topology.Exchanges)
        {
            entities.Add(
                new JsonTopologyEntity
                {
                    Kind = "exchange",
                    Name = exchange.Name,
                    Address = exchange.Address?.ToString(),
                    Flow = "inbound",
                    Properties = new Dictionary<string, object?>
                    {
                        ["type"] = exchange.Type,
                        ["durable"] = exchange.Durable,
                        ["autoDelete"] = exchange.AutoDelete,
                        ["autoProvision"] = exchange.AutoProvision
                    }
                });
        }

        // Export queues as entities (outbound - receive endpoints connect to them)
        foreach (var queue in topology.Queues)
        {
            entities.Add(
                new JsonTopologyEntity
                {
                    Kind = "queue",
                    Name = queue.Name,
                    Address = queue.Address?.ToString(),
                    Flow = "outbound",
                    Properties = new Dictionary<string, object?>
                    {
                        ["durable"] = queue.Durable,
                        ["exclusive"] = queue.Exclusive,
                        ["autoDelete"] = queue.AutoDelete,
                        ["autoProvision"] = queue.AutoProvision
                    }
                });
        }

        // Export bindings as links
        foreach (var binding in topology.Bindings)
        {
            var link = new JsonTopologyLink
            {
                Kind = "bind",
                Address = binding.Address?.ToString(),
                Source = binding.Source.Address?.ToString(),
                Target = binding switch
                {
                    RabbitMQQueueBinding qb => qb.Destination.Address?.ToString(),
                    RabbitMQExchangeBinding eb => eb.Destination.Address?.ToString(),
                    _ => null
                },
                Direction = "forward",
                Properties = new Dictionary<string, object?>
                {
                    ["routingKey"] = string.IsNullOrEmpty(binding.RoutingKey) ? null : binding.RoutingKey,
                    ["autoProvision"] = binding.AutoProvision
                }
            };

            links.Add(link);
        }

        return new JsonTopologyModel
        {
            Address = topology.Address.ToString(),
            Entities = entities,
            Links = links
        };
    }
}

/// <summary>
/// InMemory topology exporter.
/// Maps InMemory resources to the generic entity/link model:
/// - Topic → Entity (kind: "topic")
/// - Queue → Entity (kind: "queue")
/// - Binding → Link (kind: "bind")
/// </summary>
public class InMemoryTopologyExporter : ITransportTopologyExporter
{
    public bool CanExport(MessagingTransport transport) => transport is InMemoryMessagingTransport;

    public JsonTopologyModel Export(MessagingTransport transport)
    {
        var inMemoryTransport = (InMemoryMessagingTransport)transport;
        var topology = (InMemoryMessagingTopology)inMemoryTransport.Topology;

        var entities = new List<JsonTopologyEntity>();
        var links = new List<JsonTopologyLink>();

        // Export topics as entities (inbound - dispatch endpoints connect to them)
        foreach (var topic in topology.Topics)
        {
            entities.Add(
                new JsonTopologyEntity
                {
                    Kind = "topic",
                    Name = topic.Name,
                    Address = topic.Address?.ToString(),
                    Flow = "inbound"
                });
        }

        // Export queues as entities (outbound - receive endpoints connect to them)
        foreach (var queue in topology.Queues)
        {
            entities.Add(
                new JsonTopologyEntity
                {
                    Kind = "queue",
                    Name = queue.Name,
                    Address = queue.Address?.ToString(),
                    Flow = "outbound"
                });
        }

        // Export bindings as links
        foreach (var binding in topology.Bindings)
        {
            var link = new JsonTopologyLink
            {
                Kind = "bind",
                Address = binding.Address?.ToString(),
                Source = binding.Source.Address?.ToString(),
                Target = binding switch
                {
                    InMemoryQueueBinding qb => qb.Destination.Address?.ToString(),
                    InMemoryTopicBinding tb => tb.Destination.Address?.ToString(),
                    _ => null
                },
                Direction = "forward"
            };

            links.Add(link);
        }

        return new JsonTopologyModel
        {
            Address = topology.Address.ToString(),
            Entities = entities,
            Links = links
        };
    }
}

#region JSON Models

/// <summary>
/// Root model for the complete message bus topology export.
/// Contains two top-level sections: services (application-level) and transports (infrastructure-level).
/// </summary>
public class JsonBusModel
{
    /// <summary>
    /// List of services with their messaging configuration.
    /// </summary>
    public List<JsonServiceModel> Services { get; set; } = [];

    /// <summary>
    /// Transport infrastructure: endpoints and topology for each configured transport.
    /// </summary>
    public List<JsonTransportModel> Transports { get; set; } = [];
}

/// <summary>
/// Service-level model containing all application-specific messaging configuration for a single service.
/// </summary>
public class JsonServiceModel
{
    public JsonHostModel Host { get; set; } = null!;
    public List<JsonMessageTypeModel> MessageTypes { get; set; } = [];
    public List<JsonConsumerModel> Consumers { get; set; } = [];
    public JsonRoutesModel Routes { get; set; } = null!;
    public List<JsonSagaModel>? Sagas { get; set; }
}

public class JsonHostModel
{
    public string? ServiceName { get; set; }
    public string? AssemblyName { get; set; }
    public string InstanceId { get; set; } = null!;
}

public class JsonMessageTypeModel
{
    /// <summary>
    /// The unique identity URN for this message type (e.g., "urn:message:my-company:my-event").
    /// </summary>
    public string Identity { get; set; } = null!;
    public string RuntimeType { get; set; } = null!;
    public string? RuntimeTypeFullName { get; set; }
    public bool IsInterface { get; set; }
    public bool IsInternal { get; set; }
    public string? DefaultContentType { get; set; }
    public List<string>? EnclosedMessageIdentities { get; set; }
}

public class JsonConsumerModel
{
    /// <summary>
    /// The unique name of this consumer.
    /// </summary>
    public string Name { get; set; } = null!;
    public string IdentityType { get; set; } = null!;
    public string? IdentityTypeFullName { get; set; }

    /// <summary>
    /// If this consumer is a saga consumer, the name of the saga it belongs to.
    /// </summary>
    public string? SagaName { get; set; }
}

public class JsonRoutesModel
{
    public List<JsonInboundRouteModel> Inbound { get; set; } = [];
    public List<JsonOutboundRouteModel> Outbound { get; set; } = [];
}

public class JsonInboundRouteModel
{
    public InboundRouteKind Kind { get; set; }

    /// <summary>
    /// Reference to the message type by its identity URN.
    /// </summary>
    public string? MessageTypeIdentity { get; set; }

    /// <summary>
    /// Reference to the consumer by its name.
    /// </summary>
    public string? ConsumerName { get; set; }

    /// <summary>
    /// The endpoint this route is connected to.
    /// </summary>
    public JsonEndpointReference? Endpoint { get; set; }
}

public class JsonOutboundRouteModel
{
    public OutboundRouteKind Kind { get; set; }

    /// <summary>
    /// Reference to the message type by its identity URN.
    /// </summary>
    public string MessageTypeIdentity { get; set; } = null!;
    public string? Destination { get; set; }

    /// <summary>
    /// The endpoint this route is connected to.
    /// </summary>
    public JsonEndpointReference? Endpoint { get; set; }
}

/// <summary>
/// Reference to an endpoint using its natural identifiers.
/// </summary>
public class JsonEndpointReference
{
    public string Name { get; set; } = null!;

    /// <summary>
    /// The unique address URI of this endpoint.
    /// </summary>
    public string? Address { get; set; }
    public string TransportName { get; set; } = null!;
}

/// <summary>
/// Reference to a topology resource using its address URI.
/// </summary>
public class JsonTopologyResourceReference
{
    /// <summary>
    /// The unique address URI of this topology resource.
    /// </summary>
    public string? Address { get; set; }
}

public class JsonTransportModel
{
    /// <summary>
    /// Globally unique, stable identifier for this transport instance.
    /// This identifier is consistent across all services connecting to the same transport.
    ///
    /// Examples:
    /// - RabbitMQ: "rabbitmq://dev.rabbitmq.mycompany.com:5672/" (connection host/port)
    /// - Kafka: "kafka://kafka.mycompany.com:9092/" (bootstrap server)
    /// - InMemory: "inmemory://MyService/" (service/assembly name, per-process)
    /// - Azure Service Bus: "servicebus://myns.servicebus.windows.net/"
    /// </summary>
    public string Identifier { get; set; } = null!;

    /// <summary>
    /// The local display name of this transport (e.g., "RabbitMQ", "Kafka").
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The URI scheme for this transport (e.g., "rabbitmq", "kafka", "inmemory").
    /// </summary>
    public string Schema { get; set; } = null!;

    /// <summary>
    /// The concrete transport type name (e.g., "RabbitMQMessagingTransport").
    /// </summary>
    public string TransportType { get; set; } = null!;

    public List<JsonReceiveEndpointModel> ReceiveEndpoints { get; set; } = [];
    public List<JsonDispatchEndpointModel> DispatchEndpoints { get; set; } = [];

    /// <summary>
    /// The transport-neutral topology graph with entities and links.
    /// </summary>
    public JsonTopologyModel? Topology { get; set; }
}

public class JsonReceiveEndpointModel
{
    public string Name { get; set; } = null!;
    public ReceiveEndpointKind Kind { get; set; }

    /// <summary>
    /// The unique address URI of this endpoint.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Reference to the source topology resource.
    /// </summary>
    public JsonTopologyResourceReference? Source { get; set; }
}

public class JsonDispatchEndpointModel
{
    public string Name { get; set; } = null!;
    public DispatchEndpointKind Kind { get; set; }

    /// <summary>
    /// The unique address URI of this endpoint.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Reference to the destination topology resource.
    /// </summary>
    public JsonTopologyResourceReference? Destination { get; set; }
}

/// <summary>
/// Transport-neutral topology model as a graph of entities and links.
///
/// Entities represent passive messaging resources (queue, exchange, topic, subscription, consumerGroup).
/// Links represent relationships or data flow between entities (bind, subscribe, forward).
///
/// This model supports RabbitMQ, Kafka, Service Bus, in-memory, and future transports with one stable schema.
/// Transport adapters interpret entities and links according to their specific semantics.
/// </summary>
public class JsonTopologyModel
{
    /// <summary>
    /// The base address URI of this topology (e.g., "rabbitmq://localhost:5672/").
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Passive messaging resources in the topology.
    /// Examples: queue, exchange, topic, subscription, consumerGroup, partition.
    /// </summary>
    public List<JsonTopologyEntity> Entities { get; set; } = [];

    /// <summary>
    /// Relationships or data flow between entities.
    /// Examples: bind, subscribe, forward, route.
    /// </summary>
    public List<JsonTopologyLink> Links { get; set; } = [];
}

/// <summary>
/// A passive messaging resource in the topology graph.
/// Entities do not define connectivity - that is the role of links.
/// </summary>
public class JsonTopologyEntity
{
    /// <summary>
    /// The kind of entity (e.g., "queue", "exchange", "topic", "subscription", "consumerGroup").
    /// </summary>
    public string Kind { get; set; } = null!;

    /// <summary>
    /// The name of this entity within its topology.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The unique address URI of this entity (e.g., "rabbitmq://localhost:5672/q/my-queue").
    /// This serves as the entity's identifier for linking.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// The message flow from the transport's perspective:
    /// - "inbound": dispatch endpoints connect to this entity (messages flow INTO the transport)
    /// - "outbound": receive endpoints connect to this entity (messages flow OUT from the transport)
    /// Examples: exchanges/topics are inbound, queues/streams are outbound.
    /// </summary>
    public string? Flow { get; set; }

    /// <summary>
    /// Transport-specific properties for this entity.
    /// Examples: durable, exclusive, autoDelete, partitionCount, retentionPolicy.
    /// </summary>
    public Dictionary<string, object?>? Properties { get; set; }
}

/// <summary>
/// A relationship or data flow between entities in the topology graph.
/// Links define all connectivity and routing semantics.
/// </summary>
public class JsonTopologyLink
{
    /// <summary>
    /// The kind of link (e.g., "bind", "subscribe", "forward", "route").
    /// </summary>
    public string Kind { get; set; } = null!;

    /// <summary>
    /// The unique address URI of this link, if applicable.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// The source entity address URI (where messages flow from).
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// The target entity address URI (where messages flow to).
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The direction of message flow relative to Source and Target:
    /// - "forward": messages flow from Source to Target (default)
    /// - "reverse": messages flow from Target to Source
    /// - "bidirectional": messages flow in both directions
    /// </summary>
    public string? Direction { get; set; }

    /// <summary>
    /// Transport-specific properties for this link.
    /// Examples: routingKey, filter, selector, prefetchCount.
    /// </summary>
    public Dictionary<string, object?>? Properties { get; set; }
}

// Saga models
public class JsonSagaModel
{
    /// <summary>
    /// The unique name of this saga.
    /// </summary>
    public string Name { get; set; } = null!;
    public string StateType { get; set; } = null!;
    public string? StateTypeFullName { get; set; }

    /// <summary>
    /// Reference to the consumer by its name.
    /// </summary>
    public string ConsumerName { get; set; } = null!;
    public List<JsonSagaStateModel> States { get; set; } = [];
}

public class JsonSagaStateModel
{
    public string Name { get; set; } = null!;
    public bool IsInitial { get; set; }
    public bool IsFinal { get; set; }
    public JsonSagaLifeCycleModel? OnEntry { get; set; }
    public JsonSagaResponseModel? Response { get; set; }
    public List<JsonSagaTransitionModel> Transitions { get; set; } = [];
}

public class JsonSagaLifeCycleModel
{
    public List<JsonSagaEventModel>? Publish { get; set; }
    public List<JsonSagaEventModel>? Send { get; set; }
}

public class JsonSagaResponseModel
{
    public string EventType { get; set; } = null!;
    public string? EventTypeFullName { get; set; }
}

public class JsonSagaTransitionModel
{
    public string EventType { get; set; } = null!;
    public string? EventTypeFullName { get; set; }
    public string TransitionTo { get; set; } = null!;
    public SagaTransitionKind TransitionKind { get; set; }
    public bool AutoProvision { get; set; }
    public List<JsonSagaEventModel>? Publish { get; set; }
    public List<JsonSagaEventModel>? Send { get; set; }
}

public class JsonSagaEventModel
{
    public string MessageType { get; set; } = null!;
    public string? MessageTypeFullName { get; set; }
}

#endregion
