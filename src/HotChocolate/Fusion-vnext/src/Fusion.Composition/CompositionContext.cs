using System.Collections.Immutable;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal sealed class CompositionContext(
    ImmutableSortedSet<MutableSchemaDefinition> schemaDefinitions,
    ICompositionLog compositionLog)
{
    /// <summary>
    /// Gets the schema definitions.
    /// </summary>
    public ImmutableSortedSet<MutableSchemaDefinition> SchemaDefinitions { get; } = schemaDefinitions;

    /// <summary>
    /// Gets the composition log.
    /// </summary>
    public ICompositionLog Log { get; } = compositionLog;
}
