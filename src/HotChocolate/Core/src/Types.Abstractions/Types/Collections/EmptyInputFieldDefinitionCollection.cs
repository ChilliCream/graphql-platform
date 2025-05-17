using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

internal sealed class EmptyInputFieldDefinitionCollection : IReadOnlyFieldDefinitionCollection<IInputValueDefinition>
{
    private EmptyInputFieldDefinitionCollection() { }

    public IInputValueDefinition this[string name] => throw new ArgumentOutOfRangeException(nameof(name));

    public bool TryGetField(string name, [NotNullWhen(true)] out IInputValueDefinition? field)
    {
        field = null;
        return false;
    }

    public bool ContainsName(string name) => false;

    public IEnumerator<IInputValueDefinition> GetEnumerator()
    {
        yield break;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static EmptyInputFieldDefinitionCollection Instance { get; } = new();
}
