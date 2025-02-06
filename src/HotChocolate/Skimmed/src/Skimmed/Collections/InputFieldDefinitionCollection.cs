using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class InputFieldDefinitionCollection
    : FieldDefinitionCollection<InputFieldDefinition>
    , IInputFieldDefinitionCollection
    , IReadOnlyFieldDefinitionCollection<IReadOnlyInputValueDefinition>
{
    IReadOnlyInputValueDefinition IReadOnlyFieldDefinitionCollection<IReadOnlyInputValueDefinition>.this[string name]
        => this[name];

    bool IReadOnlyFieldDefinitionCollection<IReadOnlyInputValueDefinition>.TryGetField(
        string name,
        [NotNullWhen(true)] out IReadOnlyInputValueDefinition? field)
    {
        if(TryGetField(name, out var inputField))
        {
            field = inputField;
            return true;
        }

        field = null;
        return false;
    }

    IEnumerator<IReadOnlyInputValueDefinition> IEnumerable<IReadOnlyInputValueDefinition>.GetEnumerator()
        => GetEnumerator();
}
