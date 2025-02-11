using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyOutputFieldDefinitionCollection
    : ReadOnlyFieldDefinitionCollection<OutputFieldDefinition>
    , IOutputFieldDefinitionCollection
    , IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>
{
    private ReadOnlyOutputFieldDefinitionCollection(IEnumerable<OutputFieldDefinition> values)
        : base(values)
    {
    }

    IOutputFieldDefinition IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>.this[string name]
        => this[name];

    bool IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>.TryGetField(
        string name,
        [NotNullWhen(true)] out IOutputFieldDefinition? field)
    {
        if(TryGetField(name, out var f))
        {
            field = f;
            return true;
        }

        field = null;
        return false;
    }

    IEnumerator<IOutputFieldDefinition> IEnumerable<IOutputFieldDefinition>.GetEnumerator()
        => GetEnumerator();

    public static ReadOnlyOutputFieldDefinitionCollection Empty { get; } = new(Array.Empty<OutputFieldDefinition>());

    public static ReadOnlyOutputFieldDefinitionCollection From(IEnumerable<OutputFieldDefinition> values)
        => new(values);
}
