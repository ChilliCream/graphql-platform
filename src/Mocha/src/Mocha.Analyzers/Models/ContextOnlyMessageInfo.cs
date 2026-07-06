using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// Represents a message type discovered from the <c>JsonSerializerContext</c> that does not
/// have a corresponding handler in the current assembly. These types still need serializer
/// registrations for AOT compatibility.
/// </summary>
/// <param name="MessageTypeName">The fully qualified type name of the message.</param>
/// <param name="MessageTypeHierarchy">
/// The unfiltered type hierarchy of the message type (base types excluding <c>object</c>, plus all interfaces),
/// as fully qualified display strings. Filtering to registered types happens in the generator phase.
/// </param>
/// <param name="Location">
/// The equatable source location from the <c>[JsonSerializable]</c> attribute, or <see langword="null"/> if unavailable.
/// </param>
public sealed record ContextOnlyMessageInfo(
    string MessageTypeName,
    ImmutableEquatableArray<string> MessageTypeHierarchy,
    LocationInfo? Location) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"MsgContextOnly:{MessageTypeName}";
}
