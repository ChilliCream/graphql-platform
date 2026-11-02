namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides a placeholder for the body of an event-stream subscription field. Event-stream
/// fields are fulfilled by the distributed GraphQL executor and never execute their local
/// resolver, so this helper only exists to let the authored field compile.
/// </summary>
public static class EventStream
{
    /// <summary>
    /// Acts as a placeholder body for an event-stream subscription field.
    /// </summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <param name="args">
    /// The arguments for the event-stream subscription field.
    /// </param>
    /// <returns>This method never returns a value.</returns>
    /// <exception cref="NotSupportedException">
    /// Always thrown. Event-stream fields are fulfilled by the distributed GraphQL executor,
    /// not the local resolver.
    /// </exception>
    public static T Create<T>(params object?[] args)
        => throw new NotSupportedException(
            "Event-stream fields are fulfilled by the Fusion gateway broker, "
            + "not the local resolver.");
}
