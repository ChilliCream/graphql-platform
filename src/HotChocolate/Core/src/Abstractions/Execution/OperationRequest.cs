using System;
using System.Collections.Generic;
using static HotChocolate.Properties.AbstractionResources;

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
        string? documentHash,
        string? operationName,
        IReadOnlyDictionary<string, object?>? variableValues,
        IReadOnlyDictionary<string, object?>? extensions,
        IReadOnlyDictionary<string, object?>? contextData,
        IServiceProvider? services,
        GraphQLRequestFlags flags)
    {
        if (document is null && !OperationDocumentId.IsNullOrEmpty(documentId))
        {
            throw new InvalidOperationException(OperationRequest_DocumentOrIdMustBeSet);
        }

        Document = document;
        DocumentId = documentId;
        DocumentHash = documentHash;
        OperationName = operationName;
        VariableValues = variableValues;
        Extensions = extensions;
        ContextData = contextData;
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
    public OperationDocumentId? DocumentId { get; }

    /// <summary>
    /// Gets GraphQL request document hash.
    /// </summary>
    public string? DocumentHash { get; }

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
    /// Gets the services that shall be used while executing the GraphQL request.
    /// </summary>
    public IServiceProvider? Services { get; }

    /// <summary>
    /// GraphQL request flags allow to limit the GraphQL executor capabilities.
    /// </summary>
    public GraphQLRequestFlags Flags { get; }

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
    public static OperationRequest Create(
        OperationDocumentId documentId,
        string? documentHash = null,
        string? operationName = null,
        IReadOnlyDictionary<string, object?>? variableValues = null,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null,
        IServiceProvider? services = null,
        GraphQLRequestFlags flags = GraphQLRequestFlags.AllowAll)
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
    public static OperationRequest Create(
        string documentId,
        string? documentHash = null,
        string? operationName = null,
        IReadOnlyDictionary<string, object?>? variableValues = null,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null,
        IServiceProvider? services = null,
        GraphQLRequestFlags flags = GraphQLRequestFlags.AllowAll)
        => Create(
            new OperationDocumentId(documentId),
            documentHash,
            operationName,
            variableValues,
            extensions,
            contextData,
            services,
            flags);
}