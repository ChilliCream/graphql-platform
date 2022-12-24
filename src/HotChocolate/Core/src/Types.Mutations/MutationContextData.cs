namespace HotChocolate.Types;

/// <summary>
/// This internal data structure is used to store the effective mutation options of a field
/// on the context so that the type interceptor can access them.
/// </summary>
internal sealed class MutationContextData
{
    public MutationContextData(
        ObjectFieldDefinition definition,
        string? inputTypeName,
        string? inputArgumentName,
        string? payloadTypeName,
        string? payloadFieldName,
        string? payloadErrorTypeName,
        string? payloadErrorsFieldName,
        bool enabled)
    {
        Definition = definition;
        InputTypeName = inputTypeName;
        InputArgumentName = inputArgumentName;
        PayloadTypeName = payloadTypeName;
        PayloadFieldName = payloadFieldName;
        PayloadPayloadErrorTypeName = payloadErrorTypeName;
        PayloadErrorsFieldName = payloadErrorsFieldName;
        Enabled = enabled;
    }

    public string Name => Definition.Name;

    public ObjectFieldDefinition Definition { get; }

    public string? InputTypeName { get; }

    public string? InputArgumentName { get; }

    public string? PayloadFieldName { get; }

    public string? PayloadTypeName { get; }

    public string? PayloadPayloadErrorTypeName { get; }

    public string? PayloadErrorsFieldName { get; }

    public bool Enabled { get; }
}
