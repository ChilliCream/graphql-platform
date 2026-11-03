namespace HotChocolate.Fusion.Types;

/// <summary>
/// Provides access to the planning options declared by a Fusion schema.
/// </summary>
public static class PlannerOptionsExtensions
{
    extension(FusionSchemaDefinition schema)
    {
        /// <summary>
        /// Gets the planning options declared by the schema, or the default options.
        /// </summary>
        public PlannerOptions PlannerOptions
            => schema.Features.Get<PlannerOptions>() ?? PlannerOptions.Default;
    }
}
