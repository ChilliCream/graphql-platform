namespace Mocha;

/// <summary>
/// Represents the binding intent for a message type on a receive endpoint, capturing auto-binding
/// override and explicit binding configuration.
/// </summary>
/// <param name="MessageType">The message type being bound.</param>
/// <param name="AutoBind">
/// The auto-binding override for this type: null (unset, use queue/transport default), true (enabled),
/// or false (disabled). A per-type BindFrom implies AutoBind(false) unless explicitly overridden.
/// </param>
/// <param name="BindFroms">
/// The list of explicit per-type binding intents. Empty when no explicit bindings are configured
/// for this type via <c>Receives&lt;T&gt;(r =&gt; r.BindFrom(...))</c>.
/// </param>
public readonly record struct ReceiveTypeBindIntent(
    Type MessageType,
    bool? AutoBind,
    IReadOnlyList<BindFromIntent> BindFroms);

/// <summary>
/// Represents an explicit binding configuration binding a queue to a source entity with an optional
/// routing key.
/// </summary>
/// <param name="Source">The URI of the source exchange, queue, or topic to bind from.</param>
/// <param name="RoutingKey">
/// The optional routing key for the binding. When null, the binding matches all messages from the source.
/// </param>
public readonly record struct BindFromIntent(
    Uri Source,
    string? RoutingKey);
