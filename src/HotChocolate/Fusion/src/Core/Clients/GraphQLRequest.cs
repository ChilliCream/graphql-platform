using HotChocolate.Language;

namespace HotChocolate.Fusion.Clients;

public readonly struct GraphQLRequest
{
    public GraphQLRequest(
        string subgraph,
        DocumentNode document,
        ObjectValueNode? variableValues,
        ObjectValueNode? extensions)
    {
        Subgraph = subgraph;
        Document = document;
        VariableValues = variableValues;
        Extensions = extensions;
    }

    public string Subgraph { get; }

    public DocumentNode Document { get; }

    public ObjectValueNode? VariableValues { get; }

    public ObjectValueNode? Extensions { get; }
}
