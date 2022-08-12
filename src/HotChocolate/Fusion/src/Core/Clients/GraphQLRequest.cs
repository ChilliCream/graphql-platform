using HotChocolate.Language;

namespace HotChocolate.Fusion.Clients;

public readonly struct GraphQLRequest
{
    public GraphQLRequest(
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
