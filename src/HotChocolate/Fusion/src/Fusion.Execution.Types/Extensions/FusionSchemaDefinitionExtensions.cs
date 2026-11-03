using HotChocolate.Fusion.Types.Metadata;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Provides extension members for <see cref="FusionSchemaDefinition"/>.
/// </summary>
public static class FusionSchemaDefinitionExtensions
{
    extension(FusionSchemaDefinition schema)
    {
        /// <summary>
        /// Gets the cost options declared by the schema.
        /// Returns an empty record when the schema does not declare cost options.
        /// </summary>
        public FusionSchemaCostOptions CostOptions
            => schema.Features.Get<FusionSchemaCostOptions>() ?? FusionSchemaCostOptions.Empty;
    }

    internal static SchemaEnvironment? TryGetEnvironment(this ISchemaDefinition schema)
        => schema.Features.Get<SchemaEnvironment>();
}
