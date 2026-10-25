namespace Mocha.Analyzers;

/// <summary>
/// Represents the extracted metadata for a notification handler discovered during source generation.
/// </summary>
/// <param name="HandlerTypeName">The simple type name of the notification handler class.</param>
/// <param name="HandlerNamespace">The namespace containing the notification handler class.</param>
/// <param name="NotificationTypeName">The simple type name of the notification the handler processes.</param>
/// <param name="XmlDocumentation">The XML documentation captured from the handler declaration.</param>
/// <param name="Location">
/// The equatable source location of the handler type declaration, or <see langword="null"/> if unavailable.
/// </param>
public sealed record NotificationHandlerInfo(
    string HandlerTypeName,
    string HandlerNamespace,
    string NotificationTypeName,
    string? XmlDocumentation,
    LocationInfo? Location) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"Notification:{NotificationTypeName}:{HandlerTypeName}";
}
