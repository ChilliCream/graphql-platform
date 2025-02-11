using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

public interface IReadOnlyFieldDefinitionCollection<TField> : IEnumerable<TField> where TField : IFieldDefinition
{
    TField this[string name] { get; }

    bool TryGetField(string name, [NotNullWhen(true)] out TField? field);

    bool ContainsName(string name);
}
