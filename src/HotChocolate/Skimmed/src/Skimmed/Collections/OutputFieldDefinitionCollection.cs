using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Mutable;

public sealed class OutputFieldDefinitionCollection
    : FieldDefinitionCollection<OutputFieldDefinition>
    , IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>
{
    IOutputFieldDefinition IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>.this[string name]
        => this[name];

    bool IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>.TryGetField(
        string name,
        [NotNullWhen(true)] out IOutputFieldDefinition? field)
    {
        if(TryGetField(name, out var outputField))
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
