namespace Mocha;

/// <summary>
/// Represents the binding intent for a message type on a receive endpoint, capturing the
/// bind mode override for this type.
/// </summary>
/// <param name="MessageType">The message type being bound.</param>
/// <param name="BindMode">
/// The bind mode override for this type: null (unset, use queue/transport default),
/// <see cref="MessagingBindMode.Implicit"/> (enabled), or <see cref="MessagingBindMode.Explicit"/> (disabled).
/// </param>
public readonly record struct ReceiveTypeBindIntent(
    Type MessageType,
    MessagingBindMode? BindMode);
