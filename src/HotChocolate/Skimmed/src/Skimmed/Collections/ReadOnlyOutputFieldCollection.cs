using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyOutputFieldDefinitionCollection
    : ReadOnlyFieldDefinitionCollection<OutputFieldDefinition>
    , IOutputFieldDefinitionCollection
    , IReadOnlyFieldDefinitionCollection<IReadOnlyOutputFieldDefinition>
{
    private ReadOnlyOutputFieldDefinitionCollection(IEnumerable<OutputFieldDefinition> values)
        : base(values)
    {
    }

    IReadOnlyOutputFieldDefinition IReadOnlyFieldDefinitionCollection<IReadOnlyOutputFieldDefinition>.this[string name]
        => this[name];

    bool IReadOnlyFieldDefinitionCollection<IReadOnlyOutputFieldDefinition>.TryGetField(
        string name,
        [NotNullWhen(true)] out IReadOnlyOutputFieldDefinition? field)
    {
        if(TryGetField(name, out var f))
        {
            field = f;
            return true;
        }

        field = null;
        return false;
    }

    IEnumerator<IReadOnlyOutputFieldDefinition> IEnumerable<IReadOnlyOutputFieldDefinition>.GetEnumerator()
        => GetEnumerator();

    public static ReadOnlyOutputFieldDefinitionCollection Empty { get; } = new(Array.Empty<OutputFieldDefinition>());

    public static ReadOnlyOutputFieldDefinitionCollection From(IEnumerable<OutputFieldDefinition> values)
        => new(values);
}
