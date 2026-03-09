using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Mutable;

public sealed class InputFieldDefinitionCollection
    : FieldDefinitionCollection<MutableInputFieldDefinition>
    , IReadOnlyFieldDefinitionCollection<IInputValueDefinition>
{
    public InputFieldDefinitionCollection(ITypeSystemMember declaringMember)
        : base(declaringMember)
    {
    }

    IInputValueDefinition IReadOnlyFieldDefinitionCollection<IInputValueDefinition>.this[string name]
        => this[name];

    IInputValueDefinition IReadOnlyList<IInputValueDefinition>.this[int index]
        => this[index];

    bool IReadOnlyFieldDefinitionCollection<IInputValueDefinition>.TryGetField(
        string name,
        [NotNullWhen(true)] out IInputValueDefinition? field)
    {
        if (TryGetField(name, out var inputField))
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
