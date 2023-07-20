using HotChocolate.Language;
using HotChocolate.Transport;

namespace HotChocolate.Fusion.Clients;

public sealed class SubgraphGraphQLRequest
{
    public SubgraphGraphQLRequest(
        string subgraph,
        string document,
        ObjectValueNode? variableValues,
        ObjectValueNode? extensions,
        TransportFeatures requiredTransportFeatures = TransportFeatures.Standard)
    {
        Subgraph = subgraph;
        Document = document;
        VariableValues = variableValues;
        Extensions = extensions;
        RequiredTransportFeatures = requiredTransportFeatures;
    }

    public string Subgraph { get; }

    public string Document { get; }

    public ObjectValueNode? VariableValues { get; }

    public ObjectValueNode? Extensions { get; }
    
    public TransportFeatures RequiredTransportFeatures { get; }

    public static implicit operator OperationRequest(SubgraphGraphQLRequest method) 
        => new(method.Document, null, null, method.VariableValues, method.Extensions);
}

[Flags]
public enum TransportFeatures
{
    /// <summary>
    /// Standard GraphQL over HTTP POST request.
    /// </summary>
    Standard = 0,
    
    /// <summary>
    /// GraphQL multipart request.
    /// </summary>
    FileUpload = 1,
    
    /// <summary>
    /// All Features.
    /// </summary>
    All = Standard | FileUpload
}
