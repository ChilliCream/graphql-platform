using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionOutputFieldDefinitionCollection(
    ImmutableArray<FusionOutputFieldDefinition> fields)
    : FusionFieldCollection<FusionOutputFieldDefinition>(fields)
    , IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>
{
    IOutputFieldDefinition IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>.this[string name]
        => this[name];

    bool IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>.TryGetField(
        string name,
        [NotNullWhen(true)] out IOutputFieldDefinition? field)
    {
        if (TryGetField(name, out var f))
        {
            field = f;
            return true;
        }

        field = null;
        return false;
    }

    IEnumerator<IOutputFieldDefinition> IEnumerable<IOutputFieldDefinition>.GetEnumerator()
        => GetEnumerator();
}
