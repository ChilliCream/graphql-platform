namespace HotChocolate.Features;

/// <summary>
/// Provides extension methods for accessing the schema cancellation feature
/// from an <see cref="ISchemaDefinition"/>.
/// </summary>
public static class SchemaCancellationSchemaDefinitionExtensions
{
    /// <summary>
    /// Gets the <see cref="CancellationToken"/> associated with the schema.
    /// </summary>
    /// <param name="schema">
    /// The <see cref="ISchemaDefinition"/> to retrieve the cancellation token from.
    /// </param>
    /// <returns>
    /// The schema's cancellation token if the <see cref="SchemaCancellationFeature"/> is present;
    /// otherwise, <see cref="CancellationToken.None"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="schema"/> is <c>null</c>.
    /// </exception>
    public static CancellationToken GetCancellationToken(this ISchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return schema.Features.Get<SchemaCancellationFeature>()?.CancellationToken ?? CancellationToken.None;
    }
}
