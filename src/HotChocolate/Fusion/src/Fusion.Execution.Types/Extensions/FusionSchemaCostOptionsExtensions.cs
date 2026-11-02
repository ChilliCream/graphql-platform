namespace HotChocolate.Fusion.Types;

/// <summary>
/// Provides access to the cost options declared by a Fusion schema.
/// </summary>
public static class FusionSchemaCostOptionsExtensions
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
}
