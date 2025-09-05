using HotChocolate.Fusion.Types.Metadata;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Provides extension methods for <see cref="ISchemaDefinition"/>.
/// </summary>
internal static class FusionSchemaDefinitionExtensions
{
    internal static SchemaEnvironment? TryGetEnvironment(this ISchemaDefinition schema)
        => schema.Features.Get<SchemaEnvironment>();
}
