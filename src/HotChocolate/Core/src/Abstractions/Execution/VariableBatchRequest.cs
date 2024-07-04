using System;
using System.Collections.Generic;
using static HotChocolate.Properties.AbstractionResources;

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
    /// The list variable values for the GraphQL request.
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
    public VariableBatchRequest(
        IOperationDocument? document,
        OperationDocumentId? documentId,
        string? documentHash,
        string? operationName,
        IReadOnlyList<IReadOnlyDictionary<string, object?>>? variableValues,
        IReadOnlyDictionary<string, object?>? extensions,
        IReadOnlyDictionary<string, object?>? contextData,
        IServiceProvider? services,
        GraphQLRequestFlags flags)
    {
        if (document is null && OperationDocumentId.IsNullOrEmpty(documentId))
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
    /// Gets the services that shall be used while executing the GraphQL request.
    /// </summary>
    public IServiceProvider? Services { get; }

    /// <summary>
    /// GraphQL request flags allow to limit the GraphQL executor capabilities.
    /// </summary>
    public GraphQLRequestFlags Flags { get; }
}