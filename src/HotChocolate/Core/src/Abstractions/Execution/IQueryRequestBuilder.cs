using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    IQueryRequestBuilder SetInitialValue(
        object? initialValue);

    [Obsolete("Use `InitializeGlobalState`")]
    IQueryRequestBuilder SetProperties(
        Dictionary<string, object?>? properties);

    IQueryRequestBuilder InitializeGlobalState(
        Dictionary<string, object?>? globalState);

    [Obsolete("Use `InitializeGlobalState`")]
    IQueryRequestBuilder SetProperties(
        IDictionary<string, object?>? properties);

    IQueryRequestBuilder InitializeGlobalState(
        IDictionary<string, object?>? globalState);

    [Obsolete("Use `InitializeGlobalState`")]
    IQueryRequestBuilder SetProperties(
        IReadOnlyDictionary<string, object?>? properties);

    IQueryRequestBuilder InitializeGlobalState(
        IReadOnlyDictionary<string, object?>? globalState);

    [Obsolete("Use `AddGlobalState`")]
    IQueryRequestBuilder AddProperty(
        string name, object? value);

    IQueryRequestBuilder AddGlobalState<T>(
        string name, [MaybeNull] T value);

    [Obsolete("Use `TryAddGlobalState`")]
    IQueryRequestBuilder TryAddProperty(
        string name, object? value);

    IQueryRequestBuilder TryAddGlobalState<T>(
        string name, [MaybeNull] T value);

    [Obsolete("Use `SetGlobalState`")]
    IQueryRequestBuilder SetProperty(
        string name, object? value);

    IQueryRequestBuilder SetGlobalState<T>(
        string name, [MaybeNull] T value);

    [Obsolete("Use `RemoveGlobalState`")]
    IQueryRequestBuilder TryRemoveProperty(
        string name);

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

    IQueryRequestBuilder SetAllowedOperations(
        OperationType[]? allowedOperations);

    IReadOnlyQueryRequest Create();
}
