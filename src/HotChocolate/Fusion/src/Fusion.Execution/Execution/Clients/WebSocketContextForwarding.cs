using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Features;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Identifies the context value that is forwarded to a source schema WebSocket connection.
/// </summary>
public enum WebSocketContextForwardingSourceKind
{
    HttpHeader,
    ClientInitProperty
}

/// <summary>
/// Identifies the destination for a forwarded WebSocket context value.
/// </summary>
public enum WebSocketContextForwardingTargetKind
{
    InitParameter,
    UpgradeHeader
}

/// <summary>
/// Describes one value forwarded to a source schema WebSocket connection.
/// </summary>
public sealed class WebSocketContextForwardingRule
{
    /// <summary>
    /// Initializes a WebSocket context forwarding rule.
    /// </summary>
    public WebSocketContextForwardingRule(
        WebSocketContextForwardingSourceKind sourceKind,
        string sourceName,
        WebSocketContextForwardingTargetKind targetKind,
        string? targetName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceName);
        ArgumentException.ThrowIfNullOrEmpty(targetName ?? sourceName);

        SourceKind = sourceKind;
        SourceName = sourceName;
        TargetKind = targetKind;
        TargetName = targetName ?? sourceName;
    }

    /// <summary>
    /// Gets the kind of context value to read.
    /// </summary>
    public WebSocketContextForwardingSourceKind SourceKind { get; }

    /// <summary>
    /// Gets the source value name.
    /// </summary>
    public string SourceName { get; }

    /// <summary>
    /// Gets the kind of destination to write.
    /// </summary>
    public WebSocketContextForwardingTargetKind TargetKind { get; }

    /// <summary>
    /// Gets the destination value name.
    /// </summary>
    public string TargetName { get; }
}

/// <summary>
/// Configures context forwarding for source schema WebSocket connections.
/// </summary>
public sealed class WebSocketContextForwardingBuilder
{
    private readonly List<WebSocketContextForwardingRule> _rules = [];

    /// <summary>
    /// Gets the configured forwarding rules.
    /// </summary>
    public IReadOnlyList<WebSocketContextForwardingRule> Rules => _rules;

    /// <summary>
    /// Gets a value indicating whether global rules are excluded for this source schema.
    /// </summary>
    public bool ExcludeGlobalRules { get; private set; }

    /// <summary>
    /// Gets the action that can change the connection initialization payload and upgrade headers.
    /// </summary>
    public Action<OperationPlanContext, JsonObject, IDictionary<string, string>>? OnConnect { get; private set; }

    /// <summary>
    /// Adds a forwarding rule.
    /// </summary>
    public WebSocketContextForwardingBuilder AddRule(WebSocketContextForwardingRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules.Add(rule);
        return this;
    }

    /// <summary>
    /// Forwards an inbound HTTP header.
    /// </summary>
    public WebSocketContextForwardingBuilder ForwardHttpHeader(
        string sourceName,
        WebSocketContextForwardingTargetKind targetKind,
        string? targetName = null)
        => AddRule(new(
            WebSocketContextForwardingSourceKind.HttpHeader,
            sourceName,
            targetKind,
            targetName));

    /// <summary>
    /// Forwards a property from the inbound WebSocket connection initialization payload.
    /// </summary>
    public WebSocketContextForwardingBuilder ForwardClientInitProperty(
        string sourceName,
        WebSocketContextForwardingTargetKind targetKind,
        string? targetName = null)
        => AddRule(new(
            WebSocketContextForwardingSourceKind.ClientInitProperty,
            sourceName,
            targetKind,
            targetName));

    /// <summary>
    /// Excludes globally configured forwarding rules for this source schema.
    /// </summary>
    public WebSocketContextForwardingBuilder ExcludeGlobal()
    {
        ExcludeGlobalRules = true;
        return this;
    }

    /// <summary>
    /// Sets the action invoked after forwarding rules have been evaluated.
    /// </summary>
    public WebSocketContextForwardingBuilder ConfigureConnection(
        Action<OperationPlanContext, JsonObject, IDictionary<string, string>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        OnConnect = configure;
        return this;
    }

    internal WebSocketContextForwardingConfiguration Build()
        => new(
            _rules.ToArray(),
            ExcludeGlobalRules,
            OnConnect is null ? [] : [OnConnect]);
}

internal sealed class WebSocketContextForwardingConfiguration(
    IReadOnlyList<WebSocketContextForwardingRule> rules,
    bool excludeGlobalRules,
    IReadOnlyList<Action<OperationPlanContext, JsonObject, IDictionary<string, string>>> onConnect)
{
    public IReadOnlyList<WebSocketContextForwardingRule> Rules { get; } = rules;

    public bool ExcludeGlobalRules { get; } = excludeGlobalRules;

    public IReadOnlyList<Action<OperationPlanContext, JsonObject, IDictionary<string, string>>> OnConnect { get; } = onConnect;

    public bool IsEmpty => Rules.Count == 0 && OnConnect.Count == 0;

    public void Apply(
        OperationPlanContext context,
        out JsonElement initPayload,
        out Dictionary<string, string> upgradeHeaders)
    {
        if (IsEmpty)
        {
            initPayload = default;
            upgradeHeaders = [];
            return;
        }

        var payload = new JsonObject();
        upgradeHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var hasInitPayload = false;

        foreach (var rule in Rules)
        {
            if (!TryGetSourceValue(context, rule, out var value))
            {
                continue;
            }

            if (rule.TargetKind is WebSocketContextForwardingTargetKind.InitParameter)
            {
                payload[rule.TargetName] = value;
                hasInitPayload = true;
            }
            else if (TryGetHeaderValue(value, out var headerValue))
            {
                upgradeHeaders[rule.TargetName] = headerValue;
            }
        }

        foreach (var configure in OnConnect)
        {
            configure(context, payload, upgradeHeaders);
        }

        if (!hasInitPayload && OnConnect.Count == 0)
        {
            initPayload = default;
            return;
        }

        using var document = JsonDocument.Parse(payload.ToJsonString());
        initPayload = document.RootElement.Clone();
    }

    private static bool TryGetSourceValue(
        OperationPlanContext context,
        WebSocketContextForwardingRule rule,
        out JsonNode? value)
    {
        if (rule.SourceKind is WebSocketContextForwardingSourceKind.HttpHeader)
        {
            if (context.RequestContext.Features[typeof(HttpContext)] is HttpContext httpContext
                && httpContext.Request.Headers.TryGetValue(rule.SourceName, out var headerValue))
            {
                value = JsonValue.Create(headerValue.ToString());
                return true;
            }

            value = default;
            return false;
        }

        if (context.RequestContext.ContextData.TryGetValue("ISocketSession", out var session)
            && session?.GetType().GetProperty("Connection")?.GetValue(session) is IFeatureProvider connection
            && connection.Features[typeof(JsonElement)] is JsonElement payload
            && payload.ValueKind is JsonValueKind.Object
            && payload.TryGetProperty(rule.SourceName, out var property))
        {
            value = JsonNode.Parse(property.GetRawText());
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryGetHeaderValue(JsonNode? value, out string headerValue)
    {
        if (value is null)
        {
            headerValue = default!;
            return false;
        }

        if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var stringValue))
        {
            headerValue = stringValue;
            return true;
        }

        headerValue = value.ToJsonString();
        return true;
    }
}
