using System.Collections.Frozen;
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
    /// Gets a dictionary of schema definitions by name.
    /// </summary>
    public FrozenDictionary<string, MutableSchemaDefinition> SchemaDefinitionsByName { get; }
        = schemaDefinitions.ToFrozenDictionary(s => s.Name);

    /// <summary>
    /// Gets the composition log.
    /// </summary>
    public ICompositionLog Log { get; } = compositionLog;
}
