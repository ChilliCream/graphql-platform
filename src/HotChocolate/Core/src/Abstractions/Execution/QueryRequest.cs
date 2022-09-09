using System;
using System.Collections.Generic;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Execution;

/// <summary>
/// Representation of a GraphQL request.
/// </summary>
public class QueryRequest : IQueryRequest
{
    /// <summary>
    /// Initializes a new instance of <see cref="QueryRequest"/>.
    /// </summary>
    /// <param name="query">
    /// A GraphQL request document.
    /// </param>
    /// <param name="queryId">
    /// A GraphQL request document ID.
    /// </param>
    /// <param name="queryHash">
    /// A GraphQL request document hash.
    /// </param>
    /// <param name="operationName">
    /// A name of an operation in the GraphQL request document that shall be executed;
    /// or, <c>null</c> if the document only contains a single operation.
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
    /// <exception cref="QueryRequestBuilderException">
    /// <paramref name="query"/> and <paramref name="queryId"/> are both null.
    /// </exception>
    public QueryRequest(
        IQuery? query = null,
        string? queryId = null,
        string? queryHash = null,
        string? operationName = null,
        IReadOnlyDictionary<string, object?>? variableValues = null,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null,
        IServiceProvider? services = null,
        GraphQLRequestFlags flags = GraphQLRequestFlags.AllowAll)
    {
        if (query is null && queryId is null)
        {
            throw new QueryRequestBuilderException(
                AbstractionResources.QueryRequestBuilder_QueryIsNull);
        }

        Query = query;
        QueryId = queryId;
        QueryHash = queryHash;
        OperationName = operationName;
        VariableValues = variableValues;
        ContextData = contextData;
        Extensions = extensions;
        Services = services;
        Flags = flags;
    }

    /// <summary>
    /// Gets the GraphQL request document.
    /// </summary>
    public IQuery? Query { get; }

    /// <summary>
    /// A GraphQL request document ID.
    /// </summary>
    public string? QueryId { get; }

    /// <summary>
    /// A GraphQL request document hash.
    /// </summary>
    public string? QueryHash { get; }

    /// <summary>
    /// A name of an operation in the GraphQL request document that shall be executed;
    /// or, <c>null</c> if the document only contains a single operation.
    /// </summary>
    public string? OperationName { get; }

    /// <summary>
    /// The variable values for the GraphQL request.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? VariableValues { get; }

    /// <summary>
    /// The GraphQL request extension data.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// The initial global request state.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? ContextData { get; }

    /// <summary>
    /// The services that shall be used while executing the GraphQL request.
    /// </summary>
    public IServiceProvider? Services { get; }

    /// <summary>
    /// The GraphQL request flags can be used to limit the execution engine capabilities.
    /// </summary>
    public GraphQLRequestFlags Flags { get; }
}
