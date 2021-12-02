namespace HotChocolate.Types;

internal class MutationContextData
{
    public MutationContextData(
        ObjectFieldDefinition definition,
        string? inputTypeName,
        string? inputArgumentName,
        string? payloadFieldName,
        string? payloadTypeName,
        bool enabled)
    {
        Definition = definition;
        InputTypeName = inputTypeName;
        InputArgumentName = inputArgumentName;
        PayloadFieldName = payloadFieldName;
        PayloadTypeName = payloadTypeName;
        Enabled = enabled;
    }

    public NameString Name => Definition.Name;

    public ObjectFieldDefinition Definition { get; }

    public string? InputTypeName { get; }

    public string? InputArgumentName { get; }

    public string? PayloadFieldName { get; }

    public string? PayloadTypeName { get; }

    public bool Enabled { get; }
}
