using System.Collections.Immutable;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion;

internal sealed class CompositionContext(
    ImmutableArray<SchemaDefinition> schemaDefinitions,
    ICompositionLog compositionLog)
{
    /// <summary>
    /// Gets the schema definitions.
    /// </summary>
    public ImmutableArray<SchemaDefinition> SchemaDefinitions { get; } = schemaDefinitions;

    /// <summary>
    /// Gets the composition log.
    /// </summary>
    public ICompositionLog Log { get; } = compositionLog;
}
