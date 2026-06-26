using HotChocolate.Language;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @eventStream directive declares that a subscription field is fulfilled by an
/// event stream behind the distributed GraphQL executor. The directive carries the
/// payload selection set as well as the topics and broker that the executor uses to
/// resolve the stream.
/// </para>
/// <para>
/// directive @eventStream(message: FieldSelectionSet!, topics: [String!], broker: String) on FIELD_DEFINITION
/// </para>
/// </summary>
[DirectiveType(
    DirectiveNames.EventStream.Name,
    DirectiveLocation.FieldDefinition,
    IsRepeatable = false)]
public sealed class EventStreamDirective
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamDirective"/> class.
    /// </summary>
    /// <param name="message">The payload selection set.</param>
    /// <param name="topics">The topics the event stream subscribes to.</param>
    /// <param name="broker">The broker that provides the event stream.</param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="message"/> is <c>null</c>.
    /// </exception>
    public EventStreamDirective(
        SelectionSetNode message,
        IReadOnlyList<string>? topics = null,
        string? broker = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        Message = message;
        Topics = topics;
        Broker = broker;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamDirective"/> class.
    /// </summary>
    /// <param name="message">The payload selection set.</param>
    /// <param name="topics">The topics the event stream subscribes to.</param>
    /// <param name="broker">The broker that provides the event stream.</param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="message"/> is <c>null</c>.
    /// </exception>
    public EventStreamDirective(
        string message,
        IReadOnlyList<string>? topics = null,
        string? broker = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        Message = FieldSelectionSetType.ParseSelectionSet(message);
        Topics = topics;
        Broker = broker;
    }

    /// <summary>
    /// Gets the payload selection set.
    /// </summary>
    [GraphQLName(DirectiveNames.EventStream.Arguments.Message)]
    [GraphQLType<NonNullType<FieldSelectionSetType>>]
    public SelectionSetNode Message { get; }

    /// <summary>
    /// Gets the topics the event stream subscribes to.
    /// </summary>
    [GraphQLName(DirectiveNames.EventStream.Arguments.Topics)]
    public IReadOnlyList<string>? Topics { get; }

    /// <summary>
    /// Gets the broker that provides the event stream.
    /// </summary>
    [GraphQLName(DirectiveNames.EventStream.Arguments.Broker)]
    public string? Broker { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"@eventStream(message: {Message.ToString(false)[1..^1]})";
}
