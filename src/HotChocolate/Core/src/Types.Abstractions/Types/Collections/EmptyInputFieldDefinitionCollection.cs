using System.Collections;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal sealed class EmptyInputFieldDefinitionCollection : IReadOnlyFieldDefinitionCollection<IInputValueDefinition>
{
    private EmptyInputFieldDefinitionCollection() { }

    public IInputValueDefinition this[string name] => throw new ArgumentOutOfRangeException(nameof(name));

    public IInputValueDefinition this[int index] => throw new ArgumentOutOfRangeException(nameof(index));

    public int Count => 0;

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

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static EmptyInputFieldDefinitionCollection Instance { get; } = new();
}
