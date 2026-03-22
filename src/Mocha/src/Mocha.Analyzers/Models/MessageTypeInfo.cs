namespace Mocha.Analyzers;

/// <summary>
/// Represents the extracted metadata for a message type (command, query, or stream) discovered
/// during source generation.
/// </summary>
/// <param name="MessageTypeName">The simple type name of the message.</param>
/// <param name="MessageNamespace">The namespace containing the message type.</param>
/// <param name="Kind">The kind of message.</param>
/// <param name="Location">
/// The equatable source location of the message type declaration, or <see langword="null"/> if
/// unavailable.
/// </param>
public sealed record MessageTypeInfo(
    string MessageTypeName,
    string MessageNamespace,
    MessageKind Kind,
    LocationInfo? Location) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"Message:{MessageTypeName}";
}
