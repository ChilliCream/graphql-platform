namespace HotChocolate.Types;

internal class MutationContextData
{
    public MutationContextData(
        ObjectFieldDefinition definition,
        string? inputTypeName,
        string? inputArgumentName,
        string? payloadFieldName,
        string? payloadTypeName,
        string? errorTypeNamePattern,
        bool enabled)
    {
        Definition = definition;
        InputTypeName = inputTypeName;
        InputArgumentName = inputArgumentName;
        PayloadFieldName = payloadFieldName;
        PayloadTypeName = payloadTypeName;
        PayloadErrorTypeName = PayloadErrorTypeName;
        Enabled = enabled;
    }

    public NameString Name => Definition.Name;

    public ObjectFieldDefinition Definition { get; }

    public string? InputTypeName { get; }

    public string? InputArgumentName { get; }

    public string? PayloadFieldName { get; }

    public string? PayloadTypeName { get; }

    public string? PayloadErrorTypeName { get; }

    public bool Enabled { get; }
}
