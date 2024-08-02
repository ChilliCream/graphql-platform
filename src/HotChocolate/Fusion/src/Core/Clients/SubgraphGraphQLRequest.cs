using HotChocolate.Language;
using HotChocolate.Transport;

namespace HotChocolate.Fusion.Clients;

/// <summary>
/// Represents a GraphQL request that is sent to a subgraph.
/// </summary>
public sealed class SubgraphGraphQLRequest
{
    /// <summary>
    /// Initializes a new instance of <see cref="SubgraphGraphQLRequest"/>.
    /// </summary>
    /// <param name="subgraph">
    /// The name of the subgraph.
    /// </param>
    /// <param name="document">
    /// A GraphQL document string.
    /// </param>
    /// <param name="variableValues">
    /// The variable values.
    /// </param>
    /// <param name="extensions">
    /// The extensions.
    /// </param>
    /// <param name="transportFeatures">
    /// The transport features that are needed for this GraphQL request.
    /// </param>
    public SubgraphGraphQLRequest(
        string subgraph,
        string document,
        ObjectValueNode? variableValues,
        ObjectValueNode? extensions,
        TransportFeatures transportFeatures = TransportFeatures.Standard)
    {
        Subgraph = subgraph;
        Document = document;
        VariableValues = variableValues;
        Extensions = extensions;
        TransportFeatures = transportFeatures;
    }

    /// <summary>
    /// Gets the name of the subgraph.
    /// </summary>
    public string Subgraph { get; }

    /// <summary>
    /// Gets a GraphQL document string.
    /// </summary>
    public string Document { get; }

    /// <summary>
    /// Gets the variable values.
    /// </summary>
    public ObjectValueNode? VariableValues { get; }

    /// <summary>
    /// Gets the extensions.
    /// </summary>
    public ObjectValueNode? Extensions { get; }

    /// <summary>
    /// Gets the transport features that are needed for this GraphQL request.
    /// </summary>
    public TransportFeatures TransportFeatures { get; }

    /// <summary>
    /// Implicitly converts <see cref="SubgraphGraphQLRequest"/>s to <see cref="OperationRequest"/>s.
    /// </summary>
    /// <param name="request">
    /// The <see cref="SubgraphGraphQLRequest"/> to convert.
    /// </param>
    /// <returns>
    /// The converted <see cref="OperationRequest"/>.
    /// </returns>
    public static implicit operator OperationRequest(SubgraphGraphQLRequest request)
        => new(request.Document, null, null, request.VariableValues, request.Extensions);
}
