namespace HotChocolate.Skimmed;

public sealed class ReadOnlyInputFieldDefinitionCollection
    : ReadOnlyFieldDefinitionCollection<InputFieldDefinition>
    , IInputFieldDefinitionCollection
{
    private ReadOnlyInputFieldDefinitionCollection(IEnumerable<InputFieldDefinition> values)
        : base(values)
    {
    }

    public static ReadOnlyInputFieldDefinitionCollection Empty { get; } = new(Array.Empty<InputFieldDefinition>());

    public static ReadOnlyInputFieldDefinitionCollection From(IEnumerable<InputFieldDefinition> values)
        => new(values);
}
