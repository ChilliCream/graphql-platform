namespace Mocha;

/// <summary>
/// Represents the binding intent for a message type on a receive endpoint, capturing the
/// auto-binding override for this type.
/// </summary>
/// <param name="MessageType">The message type being bound.</param>
/// <param name="AutoBind">
/// The auto-binding override for this type: null (unset, use queue/transport default), true (enabled),
/// or false (disabled).
/// </param>
public readonly record struct ReceiveTypeBindIntent(
    Type MessageType,
    bool? AutoBind);
