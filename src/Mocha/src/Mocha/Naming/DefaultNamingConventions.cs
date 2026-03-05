using System.Text;
using System.Text.RegularExpressions;
using Mocha.Middlewares;
using Mocha.Sagas;

namespace Mocha;

/// <summary>
/// Provides the default naming conventions that convert handler types, message types, and route
/// metadata into kebab-case endpoint names and URN-based message identities.
/// </summary>
/// <param name="host">The host information used to derive service-scoped endpoint names.</param>
public sealed class DefaultNamingConventions(IHostInfo host) : IBusNamingConventions
{
    // Source gen
    private static readonly Regex KebabCaseRegex = new("(?<!^)(?=[A-Z])|(?<=[a-z])(?=[0-9])", RegexOptions.Compiled);

    private static readonly string[] HandlerSuffixes = { "Handler", "Consumer", "Consumer`1", "Handler`1" };

    private static readonly string[] MessageSuffixes = { "Message", "Command", "Event", "Query", "Response" };

    /// <inheritdoc />
    public string GetReceiveEndpointName(InboundRoute route, ReceiveEndpointKind kind)
    {
        ArgumentNullException.ThrowIfNull(route);

        if (!route.IsInitialized)
        {
            throw new InvalidOperationException("Route is not initialized");
        }

        return route.Kind switch
        {
            InboundRouteKind.Subscribe => (host.ServiceName is not null ? ToKebabCase(host.ServiceName) + "." : "")
                + GetReceiveEndpointName(route.Consumer.Name, kind),
            InboundRouteKind.Send => GetSendEndpointName(route.MessageType!.RuntimeType),
            InboundRouteKind.Request => GetSendEndpointName(route.MessageType!.RuntimeType),
            InboundRouteKind.Reply => "reply-endpoint",
            _ => throw new ArgumentException("Invalid inbound route kind.", nameof(route))
        };
    }

    /// <summary>
    /// Gets the receive endpoint (queue) name for a message handler type.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// - OrderCreatedHandler → order-created
    /// - OrderCreatedHandler + Fault → order-created_error
    /// - PaymentProcessedConsumer → payment-processed
    /// </remarks>
    public string GetReceiveEndpointName(Type handlerType, ReceiveEndpointKind kind)
    {
        ArgumentNullException.ThrowIfNull(handlerType);

        var baseName = FormatHandlerTypeName(handlerType);
        return ApplyEndpointKindSuffix(baseName, kind);
    }

    /// <summary>
    /// Gets the receive endpoint (queue) name for an explicit endpoint name.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// - "OrderProcessing" → order-processing
    /// - "OrderProcessing" + DeadLetter → order-processing_dead-letter
    /// </remarks>
    public string GetReceiveEndpointName(string name, ReceiveEndpointKind kind)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Endpoint name cannot be null or empty.", nameof(name));
        }

        var baseName = FormatHandlerName(name.Trim());
        return ApplyEndpointKindSuffix(baseName, kind);
    }

    /// <inheritdoc />
    public string GetSagaName(Type sagaType)
    {
        return FormatHandlerTypeName(sagaType);
    }

    /// <summary>
    /// Gets a unique instance-specific endpoint name for request-reply patterns.
    /// </summary>
    /// <remarks>
    /// Creates a temporary, unique queue for receiving replies.
    /// Format: response-{guid} (without hyphens in GUID for shorter names)
    /// </remarks>
    public string GetInstanceEndpoint(Guid consumerId)
    {
        if (consumerId == Guid.Empty)
        {
            throw new ArgumentException("Consumer ID cannot be empty.", nameof(consumerId));
        }

        // Use N format (no hyphens) for shorter queue names
        return $"response-{consumerId:N}";
    }

    /// <summary>
    /// Gets the send endpoint name for direct/command messages.
    /// </summary>
    /// <remarks>
    /// Used for point-to-point messaging (commands).
    /// Examples:
    /// - CreateOrderCommand → create-order
    /// - ProcessPaymentMessage → process-payment
    /// </remarks>
    public string GetSendEndpointName(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return FormatMessageTypeName(messageType);
    }

    /// <summary>
    /// Gets the publish endpoint (exchange) name for pub/sub messages.
    /// </summary>
    /// <remarks>
    /// Used for publish/subscribe messaging (events).
    /// Examples:
    /// - OrderCreatedEvent → order-created
    /// - PaymentProcessedMessage → payment-processed
    /// </remarks>
    public string GetPublishEndpointName(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        var @namespace = FormatMessageTypeNamespace(messageType);
        var name = FormatMessageTypeName(messageType);

        return $"{@namespace}.{name}";
    }

    /// <inheritdoc />
    public string GetMessageIdentity(Type messageType)
    {
        // TODO a) make this configurable. b) urn:<org>:<context>:<eventname> would be nicer
        var typeName = GetReadableTypeName(messageType);
        var ns = ConvertToUrnSegment(messageType.Namespace ?? "global");

        return $"urn:message:{ns}:{typeName}";
    }

    private string GetReadableTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return ConvertToUrnSegment(type.Name);
        }

        // Get the base name without the `1, `2 suffix
        var baseName = type.Name[..type.Name.IndexOf('`')];
        var convertedBaseName = ConvertToUrnSegment(baseName);

        var genericArgs = type.GetGenericArguments();

        // Handle open generics (e.g., IEventRequest<,>)
        if (type.IsGenericTypeDefinition)
        {
            var arity = genericArgs.Length;
            return arity == 1
                ? $"{convertedBaseName}[T]"
                : $"{convertedBaseName}[{string.Join(",", Enumerable.Range(1, arity).Select(i => $"T{i}"))}]";
        }

        // Handle closed generics (e.g., IEventRequest<Foo, Bar>)
        var argNames = genericArgs.Select(GetReadableTypeName);
        return $"{convertedBaseName}[{string.Join(",", argNames)}]";
    }

    private string ConvertToUrnSegment(string name)
    {
        // Convert PascalCase/namespaces to kebab-case URN-friendly format
        var result = new StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (c == '.')
            {
                result.Append('.');
            }
            else if (char.IsUpper(c))
            {
                // Add hyphen before uppercase (except at start or after a dot)
                if (i > 0 && name[i - 1] != '.')
                {
                    result.Append('-');
                }

                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Applies the appropriate suffix based on endpoint kind.
    /// </summary>
    /// <remarks>
    /// RabbitMQ conventions:
    /// - Error queues: _error (MassTransit convention)
    /// - Dead letter queues: _dead-letter (for messages that cannot be processed)
    /// - Reply queues: _reply (for request-reply patterns)
    /// </remarks>
    private static string ApplyEndpointKindSuffix(string baseName, ReceiveEndpointKind kind)
    {
        return kind switch
        {
            ReceiveEndpointKind.Default => baseName,
            ReceiveEndpointKind.Error => $"{baseName}_error",
            ReceiveEndpointKind.Skipped => $"{baseName}_skipped",
            ReceiveEndpointKind.Reply => $"{baseName}_reply",
            _ => baseName
        };
    }

    /// <summary>
    /// Formats a handler type name by removing common suffixes.
    /// </summary>
    private static string FormatHandlerTypeName(Type type)
    {
        var name = GetBaseTypeName(type);
        name = RemoveSuffixes(name, HandlerSuffixes);
        return ToKebabCase(name);
    }

    private static string FormatHandlerName(string name)
    {
        name = RemoveSuffixes(name, HandlerSuffixes);
        return ToKebabCase(name);
    }

    /// <summary>
    /// Formats a message type name by removing common suffixes.
    /// </summary>
    private static string FormatMessageTypeName(Type type)
    {
        var name = GetBaseTypeName(type);
        name = RemoveSuffixes(name, MessageSuffixes);
        return ToKebabCase(name);
    }

    private static string FormatMessageTypeNamespace(Type type)
    {
        var ns = type.Namespace;
        if (string.IsNullOrEmpty(ns))
        {
            return "";
        }

        var parts = ns.Split('.');
        return string.Join(".", parts.Select(ToKebabCase));
    }

    /// <summary>
    /// Gets the base type name, handling generic types.
    /// </summary>
    private static string GetBaseTypeName(Type type)
    {
        var name = type.Name;

        // Handle generic types: SomeHandler`1 → SomeHandler
        var backtickIndex = name.IndexOf('`');
        if (backtickIndex > 0)
        {
            name = name[..backtickIndex];
        }

        return name;
    }

    /// <summary>
    /// Removes known suffixes from a name.
    /// </summary>
    private static string RemoveSuffixes(string name, string[] suffixes)
    {
        foreach (var suffix in suffixes)
        {
            if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
            {
                return name[..^suffix.Length];
            }
        }

        return name;
    }

    /// <summary>
    /// Converts PascalCase or camelCase to kebab-case.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// - OrderCreated → order-created
    /// - XMLParser → xml-parser
    /// - Order2Created → order-2-created
    /// </remarks>
    private static string ToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Dotted names (e.g. "Mocha") → kebab each segment
        if (input.Contains('.'))
        {
            var parts = input.Split('.');
            return string.Join(".", parts.Select(ToKebabCase));
        }

        // Already kebab-case or snake_case
        if (input.Contains('-') || input.Contains('_'))
        {
            return input.ToLowerInvariant().Replace('_', '-');
        }

        var result = KebabCaseRegex.Replace(input, "-");
        return result.ToLowerInvariant();
    }
}
