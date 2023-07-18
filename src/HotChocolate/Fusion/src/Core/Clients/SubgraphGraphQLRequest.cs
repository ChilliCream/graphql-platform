using HotChocolate.Language;
using HotChocolate.Transport;

namespace HotChocolate.Fusion.Clients;

public sealed class SubgraphGraphQLRequest
{
    public SubgraphGraphQLRequest(
        string subgraph,
        string document,
        ObjectValueNode? variableValues,
        ObjectValueNode? extensions)
    {
        Subgraph = subgraph;
        Document = document;
        VariableValues = variableValues;
        Extensions = extensions;
    }

    public string Subgraph { get; }

    public string Document { get; }

    public ObjectValueNode? VariableValues { get; }

    public ObjectValueNode? Extensions { get; }
    
    public static implicit operator OperationRequest(SubgraphGraphQLRequest method) 
        => new(method.Document, null, null, method.VariableValues, method.Extensions);
}
