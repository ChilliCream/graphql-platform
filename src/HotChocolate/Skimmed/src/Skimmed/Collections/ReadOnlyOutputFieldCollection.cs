namespace HotChocolate.Skimmed;

public sealed class ReadOnlyOutputFieldDefinitionCollection
    : ReadOnlyFieldDefinitionCollection<OutputFieldDefinition>
    , IOutputFieldDefinitionCollection
{
    private ReadOnlyOutputFieldDefinitionCollection(IEnumerable<OutputFieldDefinition> values)
        : base(values)
    {
    }

    public static ReadOnlyOutputFieldDefinitionCollection Empty { get; } = new(Array.Empty<OutputFieldDefinition>());

    public static ReadOnlyOutputFieldDefinitionCollection From(IEnumerable<OutputFieldDefinition> values)
        => new(values);
}
