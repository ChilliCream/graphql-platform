using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Features;
using HotChocolate.Language;
using static HotChocolate.ExecutionAbstractionsResources;

namespace HotChocolate.Execution;

/// <summary>
/// Defines the standard GraphQL request.
/// </summary>
public sealed class OperationRequest : IOperationRequest
{
    private bool _disposed;

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
    /// <param name="errorHandlingMode">
    /// The requested error handling mode.
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
        ErrorHandlingMode? errorHandlingMode,
        JsonDocumentOwner? variableValues,
        JsonDocumentOwner? extensions,
        IReadOnlyDictionary<string, object?>? contextData,
        IFeatureCollection? features,
        IServiceProvider? services,
        RequestFlags flags)
    {
        if (document is null && OperationDocumentId.IsNullOrEmpty(documentId))
        {
            throw new ArgumentException(OperationRequest_DocumentOrIdMustBeSet, nameof(document));
        }

        if (variableValues is not null && variableValues.Document.RootElement.ValueKind is not JsonValueKind.Object)
        {
            throw new ArgumentException(OperationRequest_Variables_Must_Be_Object, nameof(variableValues));
        }

        if (extensions is not null && extensions.Document.RootElement.ValueKind is not JsonValueKind.Object)
        {
            throw new ArgumentException(OperationRequest_Extensions_Must_Be_Object, nameof(extensions));
        }

        Document = document;
        DocumentId = documentId ?? OperationDocumentId.Empty;
        DocumentHash = documentHash ?? OperationDocumentHash.Empty;
        OperationName = operationName;
        ErrorHandlingMode = errorHandlingMode;
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
    /// Gets the requested error handling mode.
    /// </summary>
    public ErrorHandlingMode? ErrorHandlingMode { get; }

    /// <summary>
    /// Gets the variable values for the GraphQL request.
    /// </summary>
    public JsonDocumentOwner? VariableValues { get; }

    /// <summary>
    /// Gets the GraphQL request extension data.
    /// </summary>
    public JsonDocumentOwner? Extensions { get; }

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

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            VariableValues?.Dispose();
            Extensions?.Dispose();
            _disposed = true;
        }
    }

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
            ErrorHandlingMode,
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
            ErrorHandlingMode,
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
            ErrorHandlingMode,
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
            ErrorHandlingMode,
            VariableValues,
            Extensions,
            ContextData,
            Features,
            Services,
            Flags);

    /// <summary>
    /// Creates a new request with the specified error handling mode.
    /// </summary>
    /// <param name="errorHandlingMode">
    /// The requested error handling mode.
    /// </param>
    /// <returns>
    /// Returns a new request with the specified error handling mode.
    /// </returns>
    public OperationRequest WithErrorHandlingMode(ErrorHandlingMode errorHandlingMode)
        => new OperationRequest(
            Document,
            DocumentId,
            DocumentHash,
            OperationName,
            errorHandlingMode,
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
    public OperationRequest WithVariableValues(JsonDocument? variableValues)
        => new OperationRequest(
            Document,
            DocumentId,
            DocumentHash,
            OperationName,
            ErrorHandlingMode,
            variableValues is null ? null : new JsonDocumentOwner(variableValues),
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
    public OperationRequest WithExtensions(JsonDocument? extensions)
        => new OperationRequest(
            Document,
            DocumentId,
            DocumentHash,
            OperationName,
            ErrorHandlingMode,
            VariableValues,
            extensions is null ? null : new JsonDocumentOwner(extensions),
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
    public OperationRequest WithContextData(IReadOnlyDictionary<string, object?>? contextData)
        => new OperationRequest(
            Document,
            DocumentId,
            DocumentHash,
            OperationName,
            ErrorHandlingMode,
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
    public OperationRequest WithFeatures(IFeatureCollection? features)
        => new OperationRequest(
            Document,
            DocumentId,
            DocumentHash,
            OperationName,
            ErrorHandlingMode,
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
    public OperationRequest WithServices(IServiceProvider? services)
        => new OperationRequest(
            Document,
            DocumentId,
            DocumentHash,
            OperationName,
            ErrorHandlingMode,
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
            ErrorHandlingMode,
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
    /// <param name="errorHandlingMode">
    /// The requested error handling mode.
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
        ErrorHandlingMode? errorHandlingMode = null,
        JsonDocument? variableValues = null,
        JsonDocument? extensions = null,
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
            errorHandlingMode,
            variableValues is null ? null : new(variableValues),
            extensions is null ? null : new(extensions),
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
    /// <param name="errorHandlingMode">
    /// The requested error handling mode.
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
        ErrorHandlingMode? errorHandlingMode = null,
        JsonDocument? variableValues = null,
        JsonDocument? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null,
        IFeatureCollection? features = null,
        IServiceProvider? services = null,
        RequestFlags flags = RequestFlags.AllowAll)
        => FromId(
            new OperationDocumentId(documentId),
            documentHash,
            operationName,
            errorHandlingMode,
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
    /// <param name="errorHandlingMode">
    /// The requested error handling mode.
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
        ErrorHandlingMode? errorHandlingMode = null,
        JsonDocument? variableValues = null,
        JsonDocument? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null,
        IFeatureCollection? features = null,
        IServiceProvider? services = null,
        RequestFlags flags = RequestFlags.AllowAll)
        => new OperationRequest(
            new OperationDocumentSourceText(sourceText),
            null,
            documentHash,
            operationName,
            errorHandlingMode,
            variableValues is null ? null : new(variableValues),
            extensions is null ? null : new(extensions),
            contextData,
            features,
            services,
            flags);
}
