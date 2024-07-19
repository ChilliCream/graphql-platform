using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public interface IFieldDefinitionCollection<TField> : ICollection<TField> where TField : IFieldDefinition
{
    TField this[string name] { get; }

    bool TryGetField(string name, [NotNullWhen(true)] out TField? field);

    bool ContainsName(string name);
}
