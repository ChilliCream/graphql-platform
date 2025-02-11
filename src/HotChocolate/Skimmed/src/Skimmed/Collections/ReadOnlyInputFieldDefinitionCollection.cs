using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyInputFieldDefinitionCollection
    : ReadOnlyFieldDefinitionCollection<InputFieldDefinition>
    , IInputFieldDefinitionCollection
    , IReadOnlyFieldDefinitionCollection<IInputValueDefinition>
{
    private ReadOnlyInputFieldDefinitionCollection(IEnumerable<InputFieldDefinition> values)
        : base(values)
    {
    }

    IInputValueDefinition IReadOnlyFieldDefinitionCollection<IInputValueDefinition>.this[string name]
        => this[name];

    bool IReadOnlyFieldDefinitionCollection<IInputValueDefinition>.TryGetField(
        string name,
        [NotNullWhen(true)] out IInputValueDefinition? field)
    {
        if(TryGetField(name, out var f))
        {
            field = f;
            return true;
        }

        field = null;
        return false;
    }

    IEnumerator<IInputValueDefinition> IEnumerable<IInputValueDefinition>.GetEnumerator()
        => GetEnumerator();

    public static ReadOnlyInputFieldDefinitionCollection Empty { get; } = new(Array.Empty<InputFieldDefinition>());

    public static ReadOnlyInputFieldDefinitionCollection From(IEnumerable<InputFieldDefinition> values)
        => new(values);
}
