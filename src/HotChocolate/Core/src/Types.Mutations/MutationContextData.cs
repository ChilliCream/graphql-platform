namespace HotChocolate.Types;

/// <summary>
/// This internal data structure is used to store the effective mutation options of a field
/// on the context so that the type interceptor can access them.
/// </summary>
internal sealed class MutationContextData(
    ObjectFieldDefinition definition,
    string? inputTypeName,
    string? inputArgumentName,
    string? payloadTypeName,
    string? payloadFieldName,
    string? payloadErrorTypeName,
    string? payloadErrorsFieldName,
    bool enabled)
{
    public string Name => Definition.Name;

    public ObjectFieldDefinition Definition { get; } = definition;

    public string? InputTypeName { get; } = inputTypeName;

    public string? InputArgumentName { get; } = inputArgumentName;

    public string? PayloadFieldName { get; } = payloadFieldName;

    public string? PayloadTypeName { get; } = payloadTypeName;

    public string? PayloadPayloadErrorTypeName { get; } = payloadErrorTypeName;

    public string? PayloadErrorsFieldName { get; } = payloadErrorsFieldName;

    public bool Enabled { get; } = enabled;
}
