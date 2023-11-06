using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Execution;

public interface IQueryRequestBuilder
{
    IQueryRequestBuilder SetQuery(
        string sourceText);

    IQueryRequestBuilder SetQuery(
        DocumentNode document);

    IQueryRequestBuilder SetQueryId(
        string? queryName);

    IQueryRequestBuilder SetQueryHash(
        string? queryHash);

    IQueryRequestBuilder SetOperation(
        string? operationName);

    IQueryRequestBuilder SetVariableValues(
        Dictionary<string, object?>? variableValues);

    IQueryRequestBuilder SetVariableValues(
        IDictionary<string, object?>? variableValues);

    IQueryRequestBuilder SetVariableValues(
        IReadOnlyDictionary<string, object?>? variableValues);

    IQueryRequestBuilder AddVariableValue(
        string name, object? value);

    IQueryRequestBuilder TryAddVariableValue(
        string name, object? value);

    IQueryRequestBuilder SetVariableValue(
        string name, object? value);

    /// <summary>
    /// Initializes the global state of the request to the given
    /// <paramref name="initialState" />.
    /// </summary>
    /// <param name="initialState">The initial state.</param>
    /// <returns>The query request builder.</returns>
    IQueryRequestBuilder InitializeGlobalState(
        Dictionary<string, object?>? initialState);

    /// <summary>
    /// Initializes the global state of the request to the given
    /// <paramref name="initialState" />.
    /// </summary>
    /// <param name="initialState">The initial state.</param>
    /// <returns>The query request builder.</returns>
    IQueryRequestBuilder InitializeGlobalState(
        IDictionary<string, object?>? initialState);

    /// <summary>
    /// Initializes the global state of the request to the given
    /// <paramref name="initialState" />.
    /// </summary>
    /// <param name="initialState">The initial state.</param>
    /// <returns>The query request builder.</returns>
    IQueryRequestBuilder InitializeGlobalState(
        IReadOnlyDictionary<string, object?>? initialState);

    /// <summary>
    /// Sets the global state for <paramref name="name" />
    /// to the specified <paramref name="value" />,
    /// or throws an exception if it already exists.
    /// </summary>
    /// <param name="name">The name of the state.</param>
    /// <param name="value">The state value.</param>
    /// <returns>The query request builder.</returns>
    /// <exception cref="System.ArgumentException">
    /// Thrown if a state value for <paramref name="name" /> already exists.
    /// </exception>
    IQueryRequestBuilder AddGlobalState(
        string name, object? value);

    /// <summary>
    /// Sets the global state for <paramref name="name" />
    /// to the specified <paramref name="value" />,
    /// if it does not yet exist.
    /// </summary>
    /// <param name="name">The name of the state.</param>
    /// <param name="value">The state value.</param>
    /// <returns>The query request builder.</returns>
    IQueryRequestBuilder TryAddGlobalState(
        string name, object? value);

    /// <summary>
    /// Sets the global state for <paramref name="name" />
    /// to the specified <paramref name="value" />.
    /// State set previously using the same <paramref name="name" />
    /// will be overwritten.
    /// </summary>
    /// <param name="name">The name of the state.</param>
    /// <param name="value">The new state value.</param>
    /// <returns>The query request builder.</returns>
    IQueryRequestBuilder SetGlobalState(
        string name, object? value);

    /// <summary>
    /// Removes the global state value for the specified
    /// <paramref name="name" />.
    /// </summary>
    /// <param name="name">The name of the state.</param>
    /// <returns>The query request builder.</returns>
    IQueryRequestBuilder RemoveGlobalState(
        string name);

    IQueryRequestBuilder SetExtensions(
        Dictionary<string, object?>? extensions);

    IQueryRequestBuilder SetExtensions(
        IDictionary<string, object?>? extensions);

    IQueryRequestBuilder SetExtensions(
        IReadOnlyDictionary<string, object?>? extensions);

    IQueryRequestBuilder AddExtension(
        string name, object? value);

    IQueryRequestBuilder TryAddExtension(
        string name, object? value);

    IQueryRequestBuilder SetExtension(
        string name, object? value);

    IQueryRequestBuilder SetServices(
        IServiceProvider? services);

    IQueryRequestBuilder TrySetServices(
        IServiceProvider? services);

    IQueryRequestBuilder SetFlags(
        GraphQLRequestFlags flags);

    IQueryRequest Create();
}
