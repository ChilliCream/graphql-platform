namespace HotChocolate.Fusion.Types;

/// <summary>
/// Provides extension methods for <see cref="ISchemaDefinition"/>.
/// </summary>
public static class FusionSchemaDefinitionExtensions
{
    internal static SchemaEnvironment? TryGetEnvironment(this ISchemaDefinition schema)
        => schema.Features.Get<SchemaEnvironment>();
}
