namespace Mocha.Analyzers;

/// <summary>
/// Represents the extracted metadata for a notification handler discovered during source generation.
/// </summary>
/// <param name="HandlerTypeName">The simple type name of the notification handler class.</param>
/// <param name="HandlerNamespace">The namespace containing the notification handler class.</param>
/// <param name="NotificationTypeName">The simple type name of the notification the handler processes.</param>
public sealed record NotificationHandlerInfo(
    string HandlerTypeName,
    string HandlerNamespace,
    string NotificationTypeName) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"Notification:{NotificationTypeName}:{HandlerTypeName}";
}
