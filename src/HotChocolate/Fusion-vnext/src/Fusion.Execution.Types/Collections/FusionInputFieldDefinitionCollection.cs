using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionInputFieldDefinitionCollection
    : FusionFieldDefinitionCollection<FusionInputFieldDefinition>
    , IReadOnlyFieldDefinitionCollection<IInputValueDefinition>
{
    public FusionInputFieldDefinitionCollection(FusionInputFieldDefinition[] fields)
        : base(fields)
    {
    }

    IInputValueDefinition IReadOnlyFieldDefinitionCollection<IInputValueDefinition>.this[string name]
        => this[name];

    bool IReadOnlyFieldDefinitionCollection<IInputValueDefinition>.TryGetField(
        string name,
        [NotNullWhen(true)] out IInputValueDefinition? field)
    {
        if (TryGetField(name, out var f))
        {
            field = f;
            return true;
        }

        field = null;
        return false;
    }

    IEnumerator<IInputValueDefinition> IEnumerable<IInputValueDefinition>.GetEnumerator()
        => GetEnumerator();

    public static FusionInputFieldDefinitionCollection Empty { get; } = new([]);
}
