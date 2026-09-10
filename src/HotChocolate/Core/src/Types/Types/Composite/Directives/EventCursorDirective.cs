namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @eventCursor directive marks the cursor of an event stream. On a subscription
/// field argument it marks the resume input that the distributed executor uses to
/// continue a stream after a previously received event. On an output field it marks
/// the value that carries the cursor of each emitted event, which a client can store
/// and later pass back to resume the stream.
/// </para>
/// <para>
/// directive @eventCursor on ARGUMENT_DEFINITION | FIELD_DEFINITION
/// </para>
/// </summary>
[DirectiveType(
    DirectiveNames.EventCursor.Name,
    DirectiveLocation.ArgumentDefinition
    | DirectiveLocation.FieldDefinition,
    IsRepeatable = false)]
public sealed class EventCursorDirective
{
    private EventCursorDirective()
    {
    }

    /// <summary>
    /// The singleton instance of the <see cref="EventCursorDirective"/> directive.
    /// </summary>
    public static EventCursorDirective Instance { get; } = new();

    /// <inheritdoc />
    public override string ToString() => "@eventCursor";
}
