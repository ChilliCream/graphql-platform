namespace HotChocolate.Types;

internal class MutationContextData
{
    public MutationContextData(
        ObjectFieldDefinition definition,
        string? inputTypeName,
        string? inputArgumentName,
        string? payloadFieldName,
        string? payloadTypeName,
        string? payloadErrorTypeName,
        string? payloadErrorsFieldName,
        bool enabled)
    {
        Definition = definition;
        InputTypeName = inputTypeName;
        InputArgumentName = inputArgumentName;
        PayloadFieldName = payloadFieldName;
        PayloadTypeName = payloadTypeName;
        PayloadPayloadErrorTypeName = payloadErrorTypeName;
        PayloadErrorsFieldName = payloadErrorsFieldName;
        Enabled = enabled;
    }

    public NameString Name => Definition.Name;

    public ObjectFieldDefinition Definition { get; }

    public string? InputTypeName { get; }

    public string? InputArgumentName { get; }

    public string? PayloadFieldName { get; }

    public string? PayloadTypeName { get; }

    public string? PayloadPayloadErrorTypeName { get; }

    public string? PayloadErrorsFieldName { get; }

    public bool Enabled { get; }
}
