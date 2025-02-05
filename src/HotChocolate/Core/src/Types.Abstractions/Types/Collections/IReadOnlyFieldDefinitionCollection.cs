using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

public interface IReadOnlyFieldDefinitionCollection<TField>
    : ICollection<TField>
    where TField : IReadOnlyFieldDefinition
{
    TField this[string name] { get; }

    bool TryGetField(string name, [NotNullWhen(true)] out TField? field);

    void Insert(int index, TField field);

    bool Remove(string name);

    void RemoveAt(int index);

    bool ContainsName(string name);

    int IndexOf(TField field);

    int IndexOf(string name);
}
