namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Factory for creating <see cref="ISourceSchemaClientScope"/> instances.
/// </summary>
public interface ISourceSchemaClientScopeFactory
{
    /// <summary>
    /// Creates a new <see cref="ISourceSchemaClientScope"/> for the given
    /// composite <paramref name="schemaDefinition"/>.
    /// </summary>
    /// <param name="schemaDefinition">
    /// The schema definition for which the client scope is created.
    /// </param>
    /// <returns>
    /// A new <see cref="ISourceSchemaClientScope"/>.
    /// </returns>
    ISourceSchemaClientScope CreateScope(ISchemaDefinition schemaDefinition);
}
