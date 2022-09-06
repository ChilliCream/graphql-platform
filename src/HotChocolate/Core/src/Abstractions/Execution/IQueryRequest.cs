using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Execution;

/// <summary>
/// Represents a GraphQL query request.
/// </summary>
public interface IQueryRequest
{
    /// <summary>
    /// Gets the GraphQL query document.
    /// </summary>
    IQuery? Query { get; }

    /// <summary>
    /// Gets an ID referring to a GraphQL persisted query.
    /// </summary>
    string? QueryId { get; }

    /// <summary>
    /// Gets the GraphQL query hash.
    /// </summary>
    string? QueryHash { get; }

    /// <summary>
    /// Gets the operation from the GraphQL query that shall be executed.
    /// </summary>
    string? OperationName { get; }

    /// <summary>
    /// Gets the GraphQL request variables.
    /// </summary>
    IReadOnlyDictionary<string, object?>? VariableValues { get; }

    /// <summary>
    /// Gets the GraphQL operation instance.
    /// </summary>
    object? InitialValue { get; }

    /// <summary>
    /// Gets custom context properties that can be passed into the GraphQL execution.
    /// </summary>
    IReadOnlyDictionary<string, object?>? ContextData { get; }

    /// <summary>
    /// Gets custom extension properties from the GraphQL request,
    /// </summary>
    IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// Gets the GraphQL request services.
    /// </summary>
    IServiceProvider? Services { get; }

    /// <summary>
    /// GraphQL request flags allow to limit the GraphQL executor.
    /// </summary>
    GraphQLRequestFlags Flags { get; }
}
