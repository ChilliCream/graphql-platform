using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public readonly struct Request
{
    public Request(
        string schemaName,
        DocumentNode document,
        ObjectValueNode? variableValues,
        ObjectValueNode? extensions)
    {
        SchemaName = schemaName;
        Document = document;
        VariableValues = variableValues;
        Extensions = extensions;
    }

    public string SchemaName { get; }

    public DocumentNode Document { get; }

    public ObjectValueNode? VariableValues { get; }

    public ObjectValueNode? Extensions { get; }
}
