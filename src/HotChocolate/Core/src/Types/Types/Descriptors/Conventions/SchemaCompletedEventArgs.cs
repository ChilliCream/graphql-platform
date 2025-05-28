namespace HotChocolate.Types.Descriptors;

/// <summary>
/// Provides data for the <see cref="IDescriptorContext.OnSchemaCreated"/> event.
/// </summary>
/// <param name="schema">
/// The schema that was created.
/// </param>
public sealed class SchemaCompletedEventArgs(Schema schema) : EventArgs
{
    /// <summary>
    /// Gets the schema that was created.
    /// </summary>
    public Schema Schema { get; } = schema ?? throw new ArgumentNullException(nameof(schema));
}
