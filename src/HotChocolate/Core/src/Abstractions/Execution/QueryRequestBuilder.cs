using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Execution;

public class QueryRequestBuilder : IQueryRequestBuilder
{
    private IQuery? _query;
    private string? _queryName;
    private string? _queryHash;
    private string? _operationName;
    private IReadOnlyDictionary<string, object?>? _readOnlyVariableValues;
    private Dictionary<string, object?>? _variableValues;
    private IReadOnlyDictionary<string, object?>? _readOnlyContextData;
    private Dictionary<string, object?>? _contextData;
    private IReadOnlyDictionary<string, object?>? _readOnlyExtensions;
    private Dictionary<string, object?>? _extensions;
    private IServiceProvider? _services;
    private GraphQLRequestFlags _flags = GraphQLRequestFlags.AllowAll;

    public IQueryRequestBuilder SetQuery(string sourceText)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            throw new ArgumentException(
                AbstractionResources.QueryRequestBuilder_QueryIsNullOrEmpty,
                nameof(sourceText));
        }

        _query = new QuerySourceText(sourceText);
        return this;
    }

    public IQueryRequestBuilder SetQuery(DocumentNode document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        _query = new QueryDocument(document);
        return this;
    }

    public IQueryRequestBuilder SetQueryId(string? queryName)
    {
        _queryName = queryName;
        return this;
    }

    public IQueryRequestBuilder SetQueryHash(string? queryHash)
    {
        _queryHash = queryHash;
        return this;
    }

    public IQueryRequestBuilder SetOperation(string? operationName)
    {
        _operationName = operationName;
        return this;
    }

    public IQueryRequestBuilder SetServices(
        IServiceProvider? services)
    {
        _services = services;
        return this;
    }

    public IQueryRequestBuilder TrySetServices(
        IServiceProvider? services)
    {
        _services ??= services;
        return this;
    }

    public IQueryRequestBuilder SetFlags(
        GraphQLRequestFlags flags)
    {
        _flags = flags;
        return this;
    }

    public IQueryRequestBuilder SetVariableValues(
        Dictionary<string, object?>? variableValues) =>
        SetVariableValues((IDictionary<string, object?>?)variableValues);

    public IQueryRequestBuilder SetVariableValues(
        IDictionary<string, object?>? variableValues)
    {
        _variableValues = variableValues is null
            ? null
            : new Dictionary<string, object?>(variableValues);
        _readOnlyVariableValues = null;
        return this;
    }

    public IQueryRequestBuilder SetVariableValues(
       IReadOnlyDictionary<string, object?>? variableValues)
    {
        _variableValues = null;
        _readOnlyVariableValues = variableValues;
        return this;
    }

    public IQueryRequestBuilder SetVariableValue(string name, object? value)
    {
        InitializeVariables();

        _variableValues![name] = value;
        return this;
    }

    public IQueryRequestBuilder AddVariableValue(
        string name, object? value)
    {
        InitializeVariables();

        _variableValues!.Add(name, value);
        return this;
    }

    public IQueryRequestBuilder TryAddVariableValue(
        string name, object? value)
    {
        InitializeVariables();

        if (!_variableValues!.ContainsKey(name))
        {
            _variableValues.Add(name, value);
        }
        return this;
    }

    [Obsolete("Use `InitializeGlobalState`")]
    public IQueryRequestBuilder SetProperties(
        Dictionary<string, object?>? properties)
        => InitializeGlobalState(properties);

    /// <inheritdoc />
    public IQueryRequestBuilder InitializeGlobalState(
        Dictionary<string, object?>? initialState)
        => InitializeGlobalState((IDictionary<string, object?>?)initialState);

    [Obsolete("Use `InitializeGlobalState`")]
    public IQueryRequestBuilder SetProperties(
        IDictionary<string, object?>? properties)
        => InitializeGlobalState(properties);

    /// <inheritdoc />
    public IQueryRequestBuilder InitializeGlobalState(
        IDictionary<string, object?>? initialState)
    {
        _contextData = initialState is null
            ? null
            : new Dictionary<string, object?>(initialState);
        _readOnlyContextData = null;
        return this;
    }

    [Obsolete("Use `InitializeGlobalState`")]
    public IQueryRequestBuilder SetProperties(
        IReadOnlyDictionary<string, object?>? properties)
        => InitializeGlobalState(properties);

    /// <inheritdoc />
    public IQueryRequestBuilder InitializeGlobalState(
        IReadOnlyDictionary<string, object?>? initialState)
    {
        _contextData = null;
        _readOnlyContextData = initialState;
        return this;
    }

    [Obsolete("Use `SetGlobalState`")]
    public IQueryRequestBuilder SetProperty(string name, object? value)
        => SetGlobalState(name, value);

    /// <inheritdoc />
    public IQueryRequestBuilder SetGlobalState(
        string name, object? value)
    {
        InitializeContextData();

        _contextData![name] = value;
        return this;
    }

    [Obsolete("Use `AddGlobalState`")]
    public IQueryRequestBuilder AddProperty(
        string name, object? value)
        => AddGlobalState(name, value);

    /// <inheritdoc />
    public IQueryRequestBuilder AddGlobalState(
        string name, object? value)
    {
        InitializeContextData();

        _contextData!.Add(name, value);
        return this;
    }

    [Obsolete("Use `TryAddGlobalState`")]
    public IQueryRequestBuilder TryAddProperty(
        string name, object? value)
        => TryAddGlobalState(name, value);

    /// <inheritdoc />
    public IQueryRequestBuilder TryAddGlobalState(
        string name, object? value)
    {
        InitializeContextData();

        if (!_contextData!.ContainsKey(name))
        {
            _contextData!.Add(name, value);
        }
        return this;
    }

    [Obsolete("Use `RemoveGlobalState`")]
    public IQueryRequestBuilder TryRemoveProperty(string name)
        => RemoveGlobalState(name);

    /// <inheritdoc />
    public IQueryRequestBuilder RemoveGlobalState(string name)
    {
        if (_readOnlyContextData is null && _contextData is null)
        {
            return this;
        }

        InitializeContextData();

        _contextData!.Remove(name);
        return this;
    }

    public IQueryRequestBuilder SetExtensions(
        Dictionary<string, object?>? extensions) =>
        SetExtensions((IDictionary<string, object?>?)extensions);

    public IQueryRequestBuilder SetExtensions(
        IDictionary<string, object?>? extensions)
    {
        _extensions = extensions is null
            ? null
            : new Dictionary<string, object?>(extensions);
        _readOnlyExtensions = null;
        return this;
    }

    public IQueryRequestBuilder SetExtensions(
        IReadOnlyDictionary<string, object?>? extensions)
    {
        _extensions = null;
        _readOnlyExtensions = extensions;
        return this;
    }

    public IQueryRequestBuilder SetExtension(string name, object? value)
    {
        InitializeExtensions();

        _extensions![name] = value;
        return this;
    }

    public IQueryRequestBuilder AddExtension(
        string name, object? value)
    {
        InitializeExtensions();

        _extensions!.Add(name, value);
        return this;
    }

    public IQueryRequestBuilder TryAddExtension(
        string name, object? value)
    {
        InitializeExtensions();

        if (!_extensions!.ContainsKey(name))
        {
            _extensions.Add(name, value);
        }
        return this;
    }

    public IQueryRequest Create()
        => new QueryRequest
        (
            query: _query,
            queryId: _queryName,
            queryHash: _queryHash,
            operationName: _operationName,
            variableValues: GetVariableValues(),
            contextData: GetContextData(),
            extensions: GetExtensions(),
            services: _services,
            flags: _flags
        );

    private IReadOnlyDictionary<string, object?> GetVariableValues()
    {
        return _variableValues ?? _readOnlyVariableValues!;
    }

    private void InitializeVariables()
    {
        if (_variableValues is null)
        {
            _variableValues = _readOnlyVariableValues is null
                ? new Dictionary<string, object?>()
                : _readOnlyVariableValues.ToDictionary(
                    t => t.Key, t => t.Value);
            _readOnlyVariableValues = null;
        }
    }

    private IReadOnlyDictionary<string, object?>? GetContextData()
    {
        return _contextData ?? _readOnlyContextData;
    }

    private void InitializeContextData()
    {
        if (_contextData is null)
        {
            _contextData = _readOnlyContextData is null
                ? new Dictionary<string, object?>()
                : _readOnlyContextData.ToDictionary(
                    t => t.Key, t => t.Value);
            _readOnlyContextData = null;
        }
    }

    private IReadOnlyDictionary<string, object?>? GetExtensions()
    {
        return _extensions ?? _readOnlyExtensions;
    }

    private void InitializeExtensions()
    {
        if (_extensions is null)
        {
            _extensions = _readOnlyExtensions is null
                ? new Dictionary<string, object?>()
                : _readOnlyExtensions.ToDictionary(
                    t => t.Key, t => t.Value);
            _readOnlyExtensions = null;
        }
    }

    public static IQueryRequest Create(string query) =>
        New().SetQuery(query).Create();

    public static QueryRequestBuilder New() => new();

    public static QueryRequestBuilder From(IQueryRequest request)
    {
        var builder = new QueryRequestBuilder
        {
            _query = request.Query,
            _queryName = request.QueryId,
            _queryHash = request.QueryHash,
            _operationName = request.OperationName,
            _readOnlyVariableValues = request.VariableValues,
            _readOnlyContextData = request.ContextData,
            _readOnlyExtensions = request.Extensions,
            _services = request.Services,
            _flags = request.Flags
        };

        if (builder._query is null && builder._queryName is null)
        {
            throw new QueryRequestBuilderException(
                AbstractionResources.QueryRequestBuilder_QueryIsNull);
        }

        return builder;
    }

    public static QueryRequestBuilder From(GraphQLRequest request)
    {
        var builder = New();

        builder
            .SetQueryId(request.QueryId)
            .SetQueryHash(request.QueryHash)
            .SetOperation(request.OperationName)
            .SetVariableValues(request.Variables)
            .SetExtensions(request.Extensions);

        if (request.Query is not null)
        {
            builder.SetQuery(request.Query);
        }

        return builder;
    }
}
