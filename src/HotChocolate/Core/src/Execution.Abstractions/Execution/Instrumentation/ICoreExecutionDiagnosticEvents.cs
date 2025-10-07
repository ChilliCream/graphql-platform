using HotChocolate.Language;

namespace HotChocolate.Execution.Instrumentation;

/// <summary>
/// Specifies the core GraphQL execution diagnostic events that can be instrumented.
/// </summary>
public interface ICoreExecutionDiagnosticEvents
{
    /// <summary>
    /// Called when starting to execute a GraphQL request with the <see cref="IRequestExecutor"/>.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed of when the execution has finished.
    /// </returns>
    IDisposable ExecuteRequest(RequestContext context);

    /// <summary>
    /// Called at the end of the execution if an exception occurred at some point,
    /// including unhandled exceptions when resolving fields.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="error">
    /// The last exception that occurred.
    /// </param>
    void RequestError(RequestContext context, Exception error);

    /// <summary>
    /// Called when a request termination error occurs within the request pipeline.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="error">
    /// The GraphQL error object.
    /// </param>
    void RequestError(RequestContext context, IError error);

    /// <summary>
    /// Called when starting to parse a document.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed of when parsing has finished.
    /// </returns>
    IDisposable ParseDocument(RequestContext context);

    /// <summary>
    /// Called when starting to validate a document.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed of when the validation has finished.
    /// </returns>
    IDisposable ValidateDocument(RequestContext context);

    /// <summary>
    /// Called if there are any document validation errors.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="errors">
    /// The GraphQL validation errors.
    /// </param>
    void ValidationErrors(RequestContext context, IReadOnlyList<IError> errors);

    /// <summary>
    /// Called when starting to coerce variables for a request.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed of when the execution has finished.
    /// </returns>
    IDisposable CoerceVariables(RequestContext context);

    /// <summary>
    /// Called when starting to execute the GraphQL operation and its resolvers.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed of when the execution has finished.
    /// </returns>
    IDisposable ExecuteOperation(RequestContext context);

    /// <summary>
    /// Called when a subscription was created.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="subscriptionId">
    /// An internal identifier for a subscription.
    /// </param>
    /// <returns>
    /// A scope that will be disposed of when the subscription has completed.
    /// </returns>
    IDisposable ExecuteSubscription(RequestContext context, ulong subscriptionId);

    /// <summary>
    /// A GraphQL request document was added to the document cache.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    void AddedDocumentToCache(RequestContext context);

    /// <summary>
    /// A GraphQL request document was retrieved from the document cache.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    void RetrievedDocumentFromCache(RequestContext context);

    /// <summary>
    /// Called when the document for a persisted operation has been read from storage.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    void RetrievedDocumentFromStorage(RequestContext context);

    /// <summary>
    /// Called when the document for a persisted operation could not be found in the
    /// operation document storage.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information
    /// about an individual GraphQL request.
    /// </param>
    /// <param name="documentId">
    /// The document id that was not found in the storage.
    /// </param>
    void DocumentNotFoundInStorage(RequestContext context, OperationDocumentId documentId);

    /// <summary>
    /// Called when a request has a non-trusted document when only trusted-documents are allowed.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information
    /// about an individual GraphQL request.
    /// </param>
    void UntrustedDocumentRejected(RequestContext context);

    /// <summary>
    /// A GraphQL request executor was created and is now able to execute GraphQL requests.
    /// </summary>
    /// <param name="name">The name of the GraphQL schema.</param>
    /// <param name="executor">The GraphQL request executor.</param>
    void ExecutorCreated(string name, IRequestExecutor executor);

    /// <summary>
    /// A GraphQL request executor was evicted and will be removed from memory.
    /// </summary>
    /// <param name="name">The name of the GraphQL schema.</param>
    /// <param name="executor">The GraphQL request executor.</param>
    void ExecutorEvicted(string name, IRequestExecutor executor);
}
