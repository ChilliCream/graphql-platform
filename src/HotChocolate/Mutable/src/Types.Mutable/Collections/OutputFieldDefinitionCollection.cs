using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Mutable;

public sealed class OutputFieldDefinitionCollection
    : FieldDefinitionCollection<MutableOutputFieldDefinition>
    , IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>
{
    public OutputFieldDefinitionCollection(ITypeSystemMember declaringMember)
        : base(declaringMember)
    {
    }

    IOutputFieldDefinition IReadOnlyList<IOutputFieldDefinition>.this[int index]
        => this[index];

    IOutputFieldDefinition IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>.this[string name]
        => this[name];

    bool IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>.TryGetField(
        string name,
        [NotNullWhen(true)] out IOutputFieldDefinition? field)
    {
        if (TryGetField(name, out var outputField))
        {
            field = outputField;
            return true;
        }

        field = null;
        return false;
    }

    IEnumerator<IOutputFieldDefinition> IEnumerable<IOutputFieldDefinition>.GetEnumerator()
        => GetEnumerator();
}
