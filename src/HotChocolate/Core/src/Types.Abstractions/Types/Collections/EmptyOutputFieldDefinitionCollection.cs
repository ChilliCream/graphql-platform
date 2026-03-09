using System.Collections;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal sealed class EmptyOutputFieldDefinitionCollection : IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>
{
    private EmptyOutputFieldDefinitionCollection() { }

    public IOutputFieldDefinition this[string name] => throw new ArgumentOutOfRangeException(nameof(name));

    public IOutputFieldDefinition this[int index] => throw new ArgumentOutOfRangeException(nameof(index));
    public int Count => 0;

    public bool TryGetField(string name, [NotNullWhen(true)] out IOutputFieldDefinition? field)
    {
        field = null;
        return false;
    }

    public bool ContainsName(string name) => false;

    public IEnumerator<IOutputFieldDefinition> GetEnumerator()
    {
        yield break;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static EmptyOutputFieldDefinitionCollection Instance { get; } = new();
}
