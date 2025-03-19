using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Mutable;

public sealed class InputFieldDefinitionCollection
    : FieldDefinitionCollection<MutableInputFieldDefinition>
    , IReadOnlyFieldDefinitionCollection<IInputValueDefinition>
{
    IInputValueDefinition IReadOnlyFieldDefinitionCollection<IInputValueDefinition>.this[string name]
        => this[name];

    bool IReadOnlyFieldDefinitionCollection<IInputValueDefinition>.TryGetField(
        string name,
        [NotNullWhen(true)] out IInputValueDefinition? field)
    {
        if(TryGetField(name, out var inputField))
        {
            field = inputField;
            return true;
        }

        field = null;
        return false;
    }

    IEnumerator<IInputValueDefinition> IEnumerable<IInputValueDefinition>.GetEnumerator()
        => GetEnumerator();
}
