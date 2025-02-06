using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyInputFieldDefinitionCollection
    : ReadOnlyFieldDefinitionCollection<InputFieldDefinition>
    , IInputFieldDefinitionCollection
    , IReadOnlyFieldDefinitionCollection<IReadOnlyInputValueDefinition>
{
    private ReadOnlyInputFieldDefinitionCollection(IEnumerable<InputFieldDefinition> values)
        : base(values)
    {
    }

    IReadOnlyInputValueDefinition IReadOnlyFieldDefinitionCollection<IReadOnlyInputValueDefinition>.this[string name]
        => this[name];

    bool IReadOnlyFieldDefinitionCollection<IReadOnlyInputValueDefinition>.TryGetField(
        string name,
        [NotNullWhen(true)] out IReadOnlyInputValueDefinition? field)
    {
        if(TryGetField(name, out var f))
        {
            field = f;
            return true;
        }

        field = null;
        return false;
    }

    IEnumerator<IReadOnlyInputValueDefinition> IEnumerable<IReadOnlyInputValueDefinition>.GetEnumerator()
        => GetEnumerator();

    public static ReadOnlyInputFieldDefinitionCollection Empty { get; } = new(Array.Empty<InputFieldDefinition>());

    public static ReadOnlyInputFieldDefinitionCollection From(IEnumerable<InputFieldDefinition> values)
        => new(values);
}
