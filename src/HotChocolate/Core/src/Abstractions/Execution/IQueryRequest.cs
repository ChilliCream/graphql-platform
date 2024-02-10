using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution;

/// <summary>
/// Representation of a GraphQL request.
/// </summary>
public interface IQueryRequest
{
    /// <summary>
    /// Gets the GraphQL request document.
    /// </summary>
    IQuery? Query { get; }

    /// <summary>
    /// Gets the GraphQL request document ID.
    /// </summary>
    string? QueryId { get; }

    /// <summary>
    /// Gets GraphQL request document hash.
    /// </summary>
    string? QueryHash { get; }

    /// <summary>
    /// A name of an operation in the GraphQL request document that shall be executed;
    /// or, <c>null</c> if the document only contains a single operation.
    /// </summary>
    string? OperationName { get; }

    /// <summary>
    /// Gets the variable values for the GraphQL request.
    /// </summary>
    IReadOnlyDictionary<string, object?>? VariableValues { get; }

    /// <summary>
    /// Gets the GraphQL request extension data.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// Gets the initial request state.
    /// </summary>
    IReadOnlyDictionary<string, object?>? ContextData { get; }

    /// <summary>
    /// Gets the services that shall be used while executing the GraphQL request.
    /// </summary>
    IServiceProvider? Services { get; }

    /// <summary>
    /// GraphQL request flags allow to limit the GraphQL executor capabilities.
    /// </summary>
    GraphQLRequestFlags Flags { get; }
}
