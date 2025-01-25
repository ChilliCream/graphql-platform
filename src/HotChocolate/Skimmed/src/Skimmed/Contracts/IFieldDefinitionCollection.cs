using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public interface IFieldDefinitionCollection<TField> : ICollection<TField> where TField : IFieldDefinition
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
