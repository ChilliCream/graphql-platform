using HotChocolate.Language;

namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides extension methods to configure the @eventStream and @eventCursor directives
/// with the fluent API.
/// </summary>
public static class EventStreamDescriptorExtensions
{
    /// <summary>
    /// <para>
    /// Applies the @eventStream directive to this subscription field to declare that it is
    /// fulfilled by an event stream behind the distributed GraphQL executor.
    /// </para>
    /// <para>
    /// @eventStream(message: "user { id }", topics: ["onUserCreated"], broker: "kafka")
    /// </para>
    /// </summary>
    /// <param name="descriptor">The object field descriptor.</param>
    /// <param name="message">
    /// The payload selection set. The outer braces are optional and will be added if not present.
    /// </param>
    /// <param name="topic">The topic the event stream subscribes to.</param>
    /// <param name="broker">The broker that provides the event stream.</param>
    /// <returns>The object field descriptor with the @eventStream directive applied.</returns>
    public static IObjectFieldDescriptor EventStream(
        this IObjectFieldDescriptor descriptor,
        string message,
        string? topic = null,
        string? broker = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(message);

        var topics = topic is null ? null : new[] { topic };

        return ApplyEventStream(descriptor, message, topics, broker);
    }

    /// <summary>
    /// <para>
    /// Applies the @eventStream directive to this subscription field to declare that it is
    /// fulfilled by an event stream behind the distributed GraphQL executor.
    /// </para>
    /// <para>
    /// @eventStream(message: "user { id }", topics: ["onUserCreated"], broker: "kafka")
    /// </para>
    /// </summary>
    /// <param name="descriptor">The object field descriptor.</param>
    /// <param name="message">
    /// The payload selection set. The outer braces are optional and will be added if not present.
    /// </param>
    /// <param name="topics">The topics the event stream subscribes to.</param>
    /// <param name="broker">The broker that provides the event stream.</param>
    /// <returns>The object field descriptor with the @eventStream directive applied.</returns>
    public static IObjectFieldDescriptor EventStream(
        this IObjectFieldDescriptor descriptor,
        string message,
        string[]? topics,
        string? broker = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(message);

        return ApplyEventStream(descriptor, message, topics, broker);
    }

    private static IObjectFieldDescriptor ApplyEventStream(
        IObjectFieldDescriptor descriptor,
        string message,
        IReadOnlyList<string>? topics,
        string? broker)
    {
        SelectionSetNode selectionSet;

        try
        {
            selectionSet = FieldSelectionSetType.ParseSelectionSet(message);
        }
        catch (SyntaxException ex)
        {
            descriptor.Extend().OnBeforeNaming(
                (ctx, _) => ctx.ReportError(
                    SchemaErrorBuilder.New()
                        .SetMessage("The field selection set syntax is invalid.")
                        .SetException(ex)
                        .Build()));
            return descriptor;
        }

        return descriptor.Directive(new EventStreamDirective(selectionSet, topics, broker));
    }

    /// <summary>
    /// <para>
    /// Applies the @eventCursor directive to this argument to mark it as the resume input
    /// that the distributed GraphQL executor uses to continue an event stream.
    /// </para>
    /// <para>
    /// @eventCursor
    /// </para>
    /// </summary>
    /// <param name="descriptor">The argument descriptor.</param>
    /// <returns>The argument descriptor with the @eventCursor directive applied.</returns>
    public static IArgumentDescriptor EventCursor(this IArgumentDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(EventCursorDirective.Instance);
    }

    /// <summary>
    /// <para>
    /// Applies the @eventCursor directive to this output field to mark it as the cursor
    /// that carries the position within an event stream.
    /// </para>
    /// <para>
    /// @eventCursor
    /// </para>
    /// </summary>
    /// <param name="descriptor">The object field descriptor.</param>
    /// <returns>The object field descriptor with the @eventCursor directive applied.</returns>
    public static IObjectFieldDescriptor EventCursor(this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(EventCursorDirective.Instance);
    }
}
