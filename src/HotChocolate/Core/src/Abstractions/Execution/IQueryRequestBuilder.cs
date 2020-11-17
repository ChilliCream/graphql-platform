using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Execution
{
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

        IQueryRequestBuilder SetProperties(
            Dictionary<string, object?>? properties);

        IQueryRequestBuilder SetProperties(
            IDictionary<string, object?>? properties);

        IQueryRequestBuilder SetProperties(
            IReadOnlyDictionary<string, object?>? properties);

        IQueryRequestBuilder AddProperty(
            string name, object? value);

        IQueryRequestBuilder TryAddProperty(
            string name, object? value);

        IQueryRequestBuilder SetProperty(
            string name, object? value);

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
}
