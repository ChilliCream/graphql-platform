using HotChocolate.Features;
using HotChocolate.Language;
using static HotChocolate.ExecutionAbstractionsResources;

namespace HotChocolate.Execution;

/// <summary>
/// Defines the standard GraphQL request.
/// </summary>
public sealed class OperationRequest : IOperationRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationRequest" /> class.
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
    /// A name of an operation in the GraphQL request document that shall be executed;
    /// </param>
    /// <param name="variableValues">
    /// The variable values for the GraphQL request.
    /// </param>
    /// <param name="extensions">
    /// The GraphQL request extension data.
    /// </param>
    /// <param name="contextData">
    /// The initial global request state.
    /// </param>
    /// <param name="features">
    /// The request features.
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
    public OperationRequest(
        IOperationDocument? document,
        OperationDocumentId? documentId,
        OperationDocumentHash? documentHash,
        string? operationName,
        IReadOnlyDictionary<string, object?>? variableValues,
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
    /// Gets the variable values for the GraphQL request.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? VariableValues { get; }

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
    /// Creates a new request with the specified document.
    /// </summary>
    /// <param name="document">
    /// The GraphQL request document.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified document.
    /// </returns>
    public OperationRequest WithDocument(IOperationDocument document)
        => new OperationRequest(
            document,
            DocumentId,
            DocumentHash,
            OperationName,
            VariableValues,
            Extensions,
            ContextData,
            Features,
            Services,
            Flags);

    /// <summary>
    /// Creates a new request with the specified document ID.
    /// </summary>
    /// <param name="documentId">
    /// The ID of the persisted operation document.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified document ID.
    /// </returns>
    public OperationRequest WithDocumentId(OperationDocumentId documentId)
        => new OperationRequest(
            Document,
            documentId,
            DocumentHash,
            OperationName,
            VariableValues,
            Extensions,
            ContextData,
            Features,
            Services,
            Flags);

    /// <summary>
    /// Creates a new request with the specified document hash.
    /// </summary>
    /// <param name="documentHash">
    /// The hash of the persisted operation document.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified document hash.
    /// </returns>
    public OperationRequest WithDocumentHash(OperationDocumentHash documentHash)
        => new OperationRequest(
            Document,
            DocumentId,
            documentHash,
            OperationName,
            VariableValues,
            Extensions,
            ContextData,
            Features,
            Services,
            Flags);

    /// <summary>
    /// Creates a new request with the specified operation name.
    /// </summary>
    /// <param name="operationName">
    /// The name of the operation that shall be executed.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified operation name.
    /// </returns>
    public OperationRequest WithOperationName(string operationName)
        => new OperationRequest(
            Document,
            DocumentId,
            DocumentHash,
            operationName,
            VariableValues,
            Extensions,
            ContextData,
            Features,
            Services,
            Flags);

    /// <summary>
    /// Creates a new request with the specified variable values.
    /// </summary>
    /// <param name="variableValues">
    /// The variable values that shall be used while executing the operation.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified variable values.
    /// </returns>
    public OperationRequest WithVariableValues(IReadOnlyDictionary<string, object?> variableValues)
        => new OperationRequest(
            Document,
            DocumentId,
            DocumentHash,
            OperationName,
            variableValues,
            Extensions,
            ContextData,
            Features,
            Services,
            Flags);

    /// <summary>
    /// Creates a new request with the specified extensions.
    /// </summary>
    /// <param name="extensions">
    /// The extensions that shall be used while executing the operation.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified extensions.
    /// </returns>
    public OperationRequest WithExtensions(IReadOnlyDictionary<string, object?> extensions)
        => new OperationRequest(
            Document,
            DocumentId,
            DocumentHash,
            OperationName,
            VariableValues,
            extensions,
            ContextData,
            Features,
            Services,
            Flags);

    /// <summary>
    /// Creates a new request with the specified context data.
    /// </summary>
    /// <param name="contextData">
    /// The context data that shall be used while executing the operation.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified context data.
    /// </returns>
    public OperationRequest WithContextData(IReadOnlyDictionary<string, object?> contextData)
        => new OperationRequest(
            Document,
            DocumentId,
            DocumentHash,
            OperationName,
            VariableValues,
            Extensions,
            contextData,
            Features,
            Services,
            Flags);

    /// <summary>
    /// Creates a new request with the specified features.
    /// </summary>
    /// <param name="features">
    /// The request features that shall be used while executing the operation.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified features.
    /// </returns>
    public OperationRequest WithFeatures(IFeatureCollection features)
        => new OperationRequest(
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

    /// <summary>
    /// Creates a new request with the specified services.
    /// </summary>
    /// <param name="services">
    /// The services that shall be used while executing the operation.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified services.
    /// </returns>
    public OperationRequest WithServices(IServiceProvider services)
        => new OperationRequest(
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
    /// Creates a new request with the specified flags.
    /// </summary>
    /// <param name="flags">
    /// The request flags.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified flags.
    /// </returns>
    public OperationRequest WithFlags(RequestFlags flags)
        => new OperationRequest(
            Document,
            DocumentId,
            DocumentHash,
            OperationName,
            VariableValues,
            Extensions,
            ContextData,
            Features,
            Services,
            flags);

    /// <summary>
    /// Creates a persisted operation request.
    /// </summary>
    /// <param name="documentId">
    /// The ID of the persisted operation document.
    /// </param>
    /// <param name="documentHash">
    /// The hash of the persisted operation document.
    /// </param>
    /// <param name="operationName">
    /// The name of the operation that shall be executed.
    /// </param>
    /// <param name="variableValues">
    /// The variable values for the operation.
    /// </param>
    /// <param name="extensions">
    /// The extensions for the operation.
    /// </param>
    /// <param name="contextData">
    /// The context data for the operation.
    /// </param>
    /// <param name="features">
    /// The request features.
    /// </param>
    /// <param name="services">
    /// The services that shall be used while executing the operation.
    /// </param>
    /// <param name="flags">
    /// The request flags.
    /// </param>
    /// <returns>
    /// Returns a new persisted operation request.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="documentId"/> is <c>null</c>.
    /// </exception>
    public static OperationRequest FromId(
        OperationDocumentId documentId,
        OperationDocumentHash? documentHash = null,
        string? operationName = null,
        IReadOnlyDictionary<string, object?>? variableValues = null,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null,
        IFeatureCollection? features = null,
        IServiceProvider? services = null,
        RequestFlags flags = RequestFlags.AllowAll)
    {
        if (OperationDocumentId.IsNullOrEmpty(documentId))
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        return new OperationRequest(
            null,
            documentId,
            documentHash,
            operationName,
            variableValues,
            extensions,
            contextData,
            features,
            services,
            flags);
    }

    /// <summary>
    /// Creates a persisted operation request.
    /// </summary>
    /// <param name="documentId">
    /// The ID of the persisted operation document.
    /// </param>
    /// <param name="documentHash">
    /// The hash of the persisted operation document.
    /// </param>
    /// <param name="operationName">
    /// The name of the operation that shall be executed.
    /// </param>
    /// <param name="variableValues">
    /// The variable values for the operation.
    /// </param>
    /// <param name="extensions">
    /// The extensions for the operation.
    /// </param>
    /// <param name="contextData">
    /// The context data for the operation.
    /// </param>
    /// <param name="features">
    /// The request features.
    /// </param>
    /// <param name="services">
    /// The services that shall be used while executing the operation.
    /// </param>
    /// <param name="flags">
    /// The request flags.
    /// </param>
    /// <returns>
    /// Returns a new persisted operation request.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="documentId"/> is <c>null</c>.
    /// </exception>
    public static OperationRequest FromId(
        string documentId,
        OperationDocumentHash? documentHash = null,
        string? operationName = null,
        IReadOnlyDictionary<string, object?>? variableValues = null,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null,
        IFeatureCollection? features = null,
        IServiceProvider? services = null,
        RequestFlags flags = RequestFlags.AllowAll)
        => FromId(
            new OperationDocumentId(documentId),
            documentHash,
            operationName,
            variableValues,
            extensions,
            contextData,
            features,
            services,
            flags);

    /// <summary>
    /// Creates a GraphQL request from an operation document source text.
    /// </summary>
    /// <param name="sourceText">
    /// The GraphQL operation-document source text.
    /// </param>
    /// <param name="documentHash">
    /// The hash of the persisted operation document.
    /// </param>
    /// <param name="operationName">
    /// The name of the operation that shall be executed.
    /// </param>
    /// <param name="variableValues">
    /// The variable values for the operation.
    /// </param>
    /// <param name="extensions">
    /// The extensions for the operation.
    /// </param>
    /// <param name="contextData">
    /// The context data for the operation.
    /// </param>
    /// <param name="features">
    /// The request features.
    /// </param>
    /// <param name="services">
    /// The services that shall be used while executing the operation.
    /// </param>
    /// <param name="flags">
    /// The request flags.
    /// </param>
    /// <returns>
    /// Returns a new persisted operation request.
    /// </returns>
    public static OperationRequest FromSourceText(
        string sourceText,
        OperationDocumentHash? documentHash = null,
        string? operationName = null,
        IReadOnlyDictionary<string, object?>? variableValues = null,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null,
        IFeatureCollection? features = null,
        IServiceProvider? services = null,
        RequestFlags flags = RequestFlags.AllowAll)
        => new OperationRequest(
            new OperationDocumentSourceText(sourceText),
            null,
            documentHash,
            operationName,
            variableValues,
            extensions,
            contextData,
            features,
            services,
            flags);
}
