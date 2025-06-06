using HotChocolate.Features;
using HotChocolate.Language;
using static HotChocolate.ExecutionAbstractionsResources;

namespace HotChocolate.Execution;

/// <summary>
/// Defines a GraphQL operation request that shall be executed as many times as there are variable sets.
/// </summary>
public sealed class VariableBatchRequest : IOperationRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VariableBatchRequest" /> class.
    /// </summary>
    /// <param name="document">
    /// A GraphQL request document.
    /// </param>
    /// <param name="documentId">
    /// A GraphQL request document ID.
    /// </param>
    /// <param name="documentHash">
    /// A GraphQL request document hash.
    /// </param>
    /// <param name="operationName">
    /// A name of an operation in the GraphQL request document that shall be executed.
    /// </param>
    /// <param name="variableValues">
    /// The list of variable values for the GraphQL request.
    /// </param>
    /// <param name="extensions">
    /// The GraphQL request extension data.
    /// </param>
    /// <param name="contextData">
    /// The initial global request state.
    /// </param>
    /// <param name="features">
    /// The features that shall be used while executing the GraphQL request.
    /// </param>
    /// <param name="services">
    /// The services that shall be used while executing the GraphQL request.
    /// </param>
    /// <param name="flags">
    /// The GraphQL request flags can be used to limit the execution engine capabilities.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="document"/> and <paramref name="documentId"/> are both null.
    /// </exception>
    public VariableBatchRequest(
        IOperationDocument? document,
        OperationDocumentId? documentId,
        OperationDocumentHash? documentHash,
        string? operationName,
        IReadOnlyList<IReadOnlyDictionary<string, object?>>? variableValues,
        IReadOnlyDictionary<string, object?>? extensions,
        IReadOnlyDictionary<string, object?>? contextData,
        IFeatureCollection? features,
        IServiceProvider? services,
        RequestFlags flags)
    {
        if (document is null && OperationDocumentId.IsNullOrEmpty(documentId))
        {
            throw new InvalidOperationException(OperationRequest_DocumentOrIdMustBeSet);
        }

        Document = document;
        DocumentId = documentId ?? OperationDocumentId.Empty;
        DocumentHash = documentHash ?? OperationDocumentHash.Empty;
        OperationName = operationName;
        VariableValues = variableValues;
        Extensions = extensions;
        ContextData = contextData;
        Features = features?.ToReadOnly() ?? FeatureCollection.Empty;
        Services = services;
        Flags = flags;
    }

    /// <summary>
    /// Gets the GraphQL request document.
    /// </summary>
    public IOperationDocument? Document { get; }

    /// <summary>
    /// Gets the GraphQL request document ID.
    /// </summary>
    public OperationDocumentId DocumentId { get; }

    /// <summary>
    /// Gets GraphQL request document hash.
    /// </summary>
    public OperationDocumentHash DocumentHash { get; }

    /// <summary>
    /// A name of an operation in the GraphQL request document that shall be executed;
    /// or, <c>null</c> if the document only contains a single operation.
    /// </summary>
    public string? OperationName { get; }

    /// <summary>
    /// Gets a list of variable values for the GraphQL request.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>>? VariableValues { get; }

    /// <summary>
    /// Gets the GraphQL request extension data.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// Gets the initial request state.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? ContextData { get; }

    /// <summary>
    /// Gets the features that shall be used while executing the GraphQL request.
    /// </summary>
    public IFeatureCollection Features { get; }

    /// <summary>
    /// Gets the services that shall be used while executing the GraphQL request.
    /// </summary>
    public IServiceProvider? Services { get; }

    /// <summary>
    /// GraphQL request flags allow limiting the GraphQL executor capabilities.
    /// </summary>
    public RequestFlags Flags { get; }

    /// <summary>
    /// Creates a new request with the specified services.
    /// </summary>
    /// <param name="services">
    /// The services that shall be used while executing the operation.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified services.
    /// </returns>
    public VariableBatchRequest WithServices(IServiceProvider services)
        => new(
            Document,
            DocumentId,
            DocumentHash,
            OperationName,
            VariableValues,
            Extensions,
            ContextData,
            Features,
            services,
            Flags);

    /// <summary>
    /// Creates a new request with the specified features.
    /// </summary>
    /// <param name="features">
    /// The features that shall be used while executing the GraphQL request.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified features.
    /// </returns>
    public VariableBatchRequest WithFeatures(IFeatureCollection features)
        => new(
            Document,
            DocumentId,
            DocumentHash,
            OperationName,
            VariableValues,
            Extensions,
            ContextData,
            features,
            Services,
            Flags);
}
