using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Mocha.Features;

namespace Mocha;

/// <summary>
/// Represents a registered message type in the messaging system, holding identity, serialization, and type hierarchy metadata.
/// </summary>
public sealed class MessageType
{
    /// <summary>
    /// Gets a value indicating whether the message type has been fully initialized with its type hierarchy and enclosed types.
    /// </summary>
    public bool IsCompleted { get; private set; }

    private Type[]? _enclosedTypes;

    private IMessageSerializerRegistry _serializerRegistry = null!;

    private ImmutableDictionary<MessageContentType, IMessageSerializer> _serializer
        = ImmutableDictionary<MessageContentType, IMessageSerializer>.Empty;

    /// <summary>
    /// Gets the URN-based identity string that uniquely identifies this message type on the wire.
    /// </summary>
    public string Identity { get; private set; } = null!;

    /// <summary>
    /// Gets the CLR type represented by this message type.
    /// </summary>
    public Type RuntimeType { get; private set; } = null!;

    /// <summary>
    /// Gets the message types in the type hierarchy (base types and interfaces) that are also registered as message types.
    /// </summary>
    public ImmutableArray<MessageType> EnclosedMessageTypes { get; private set; } = [];

    /// <summary>
    /// Gets the identity strings of all types in the hierarchy, used for wire-level message matching.
    /// </summary>
    public ImmutableArray<string> EnclosedMessageIdentities { get; private set; } = [];

    /// <summary>
    /// Gets the default content type used for serialization, or <c>null</c> to use the system default.
    /// </summary>
    public MessageContentType? DefaultContentType { get; private set; }

    /// <summary>
    /// Gets the feature collection associated with this message type, providing transport-specific extensibility.
    /// </summary>
    public IFeatureCollection Features { get; private set; } = FeatureCollection.Empty;

    /// <summary>
    /// Gets a value indicating whether the underlying CLR type is an interface.
    /// </summary>
    public bool IsInterface { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this message type is marked as internal (not exposed for external routing).
    /// </summary>
    public bool IsInternal { get; private set; }

    /// <summary>
    /// Initializes this message type from configuration, applying conventions and registering outbound routes.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The configuration to initialize from.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration is missing a required identity, runtime type, or serializer registry.
    /// </exception>
    public void Initialize(IMessagingConfigurationContext context, MessageTypeConfiguration configuration)
    {
        context.Conventions.Configure(context, configuration);

        Identity = configuration.Identity ?? throw new InvalidOperationException("Message requires and identity");
        RuntimeType =
            configuration.RuntimeType ?? throw new InvalidOperationException("Message requires a runtime type");
        IsInterface = RuntimeType.IsInterface;
        IsInternal = configuration.IsInternal;
        DefaultContentType = configuration.DefaultContentType;

        Features = configuration.GetFeatures().ToReadOnly();

        _serializerRegistry =
            context.Messages.Serializers ?? throw new InvalidOperationException("Serializer registry is required");

        _serializer = configuration.MessageSerializer.ToImmutableDictionary(k => k.Key, v => v.Value);

        _enclosedTypes = configuration.EnclosedTypes;

        foreach (var routeConfiguration in configuration.Routes)
        {
            var outboundRoute = new OutboundRoute();
            routeConfiguration.MessageType = this;
            outboundRoute.Initialize(context, routeConfiguration);
            context.Router.AddOrUpdate(outboundRoute);
        }
    }

    /// <summary>
    /// Gets a serializer for the specified content type, caching the result for subsequent calls.
    /// </summary>
    /// <param name="contentType">The content type to get a serializer for.</param>
    /// <returns>A serializer for the content type, or <c>null</c> if none is available.</returns>
    public IMessageSerializer? GetSerializer(MessageContentType contentType)
    {
        if (_serializer.TryGetValue(contentType, out var serializer))
        {
            return serializer;
        }

        serializer = _serializerRegistry.GetSerializer(contentType, RuntimeType);
        if (serializer is null)
        {
            return null;
        }

        return ImmutableInterlocked.GetOrAdd(ref _serializer, contentType, serializer);
    }

    /// <summary>
    /// Completes initialization by resolving the full type hierarchy and registering enclosed message types.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "GetInterfaces is called on statically-referenced message types registered at startup.")]
    public void Complete(IMessagingConfigurationContext context)
    {
        var enclosedMessageTypes = ImmutableArray.CreateBuilder<MessageType>();
        var enclosedMessageIdentities = ImmutableArray.CreateBuilder<string>();

        if (_enclosedTypes is not null)
        {
            // AOT path: pre-sorted registered types only, no framework types
            foreach (var type in _enclosedTypes)
            {
                var mt = context.Messages.GetOrAdd(context, type);
                enclosedMessageTypes.Add(mt);
                enclosedMessageIdentities.Add(mt.Identity);
            }

            // Response types already registered by generator — skip discovery
        }
        else
        {
            // Reflection path: existing code, completely unchanged
            var allTypes = GetAllTypesInHierarchy(RuntimeType, context);

            // Sort by specificity (most specific first)
            var sortedTypes = allTypes
                .OrderByDescending(t => allTypes.Count(other => t != other && t.IsAssignableTo(other)))
                .ToList();

            foreach (var type in sortedTypes)
            {
                if (IsFrameworkBaseType(type))
                {
                    // Don't register framework base types as standalone message types.
                    // Only include their identity string for wire-level message matching.
                    enclosedMessageIdentities.Add(context.Naming.GetMessageIdentity(type));
                }
                else
                {
                    var mt = context.Messages.GetOrAdd(context, type);
                    enclosedMessageTypes.Add(mt);
                    enclosedMessageIdentities.Add(mt.Identity);
                }
            }

            var interfaces = RuntimeType.GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEventRequest<>))
                {
                    var responseType = interfaceType.GetGenericArguments()[0];
                    context.Messages.GetOrAdd(context, responseType);
                }
            }
        }

        EnclosedMessageTypes = enclosedMessageTypes.ToImmutableArray();
        EnclosedMessageIdentities = enclosedMessageIdentities.ToImmutableArray();

        IsCompleted = true;
    }

    /// <summary>
    /// Creates a description of this message type for visualization and diagnostic purposes.
    /// </summary>
    /// <returns>A <see cref="MessageTypeDescription"/> representing this message type.</returns>
    public MessageTypeDescription Describe()
    {
        return new MessageTypeDescription(
            Identity,
            DescriptionHelpers.GetTypeName(RuntimeType),
            RuntimeType.FullName,
            IsInterface,
            IsInternal,
            DefaultContentType?.Value,
            EnclosedMessageIdentities.IsDefaultOrEmpty ? null : EnclosedMessageIdentities);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Metadata read on statically-referenced types is AOT-safe.")]
    private static List<Type> GetAllTypesInHierarchy(Type type, IMessagingConfigurationContext context)
    {
        var interfaces = type.GetInterfaces();

        var types = new List<Type>(interfaces.Length + 1);

        var currentType = type;

        while (currentType is not null && currentType != typeof(object))
        {
            if (IsRelevantType(currentType, context) && !types.Contains(currentType))
            {
                types.Add(currentType);
            }

            currentType = currentType.BaseType;
        }

        foreach (var interfaceType in type.GetInterfaces())
        {
            if (IsRelevantType(interfaceType, context) && !types.Contains(interfaceType))
            {
                types.Add(interfaceType);
            }
        }

        return types;
    }

    private static bool IsRelevantType(Type type, IMessagingConfigurationContext context)
    {
        return context.Messages.IsRegistered(type) || IsFrameworkBaseType(type);
    }

    private static bool IsFrameworkBaseType(Type type)
    {
        return type == typeof(IEventRequest)
            || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEventRequest<>));
    }
}
