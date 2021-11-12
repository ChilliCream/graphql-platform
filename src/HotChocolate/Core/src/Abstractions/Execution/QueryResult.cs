using System;
using System.Collections.Generic;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Execution;

/// <summary>
/// Represents a query result object.
/// </summary>
public sealed class QueryResult
    : IQueryResult
    , IReadOnlyQueryResult
{
    private readonly IDisposable? _disposable;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="QueryResult"/>.
    /// </summary>
    public QueryResult(
        IReadOnlyDictionary<string, object?>? data,
        IReadOnlyList<IError>? errors,
        IReadOnlyDictionary<string, object?>? extension = null,
        IReadOnlyDictionary<string, object?>? contextData = null,
        string? label = null,
        Path? path = null,
        bool? hasNext = null,
        IDisposable? resultMemoryOwner = null)
    {
        if (data is null && errors is null && hasNext is not false)
        {
            throw new ArgumentException(
                AbstractionResources.QueryResult_DataAndResultAreNull,
                nameof(data));
        }

        Data = data;
        Errors = errors;
        Extensions = extension;
        ContextData = contextData;
        Label = label;
        Path = path;
        HasNext = hasNext;
        _disposable = resultMemoryOwner;
    }

    /// <inheritdoc />
    public string? Label { get; }

    /// <inheritdoc />
    public Path? Path { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? Data { get; }

    /// <inheritdoc />
    public IReadOnlyList<IError>? Errors { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? ContextData { get; }

    /// <inheritdoc />
    public bool? HasNext { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> ToDictionary()
    {
        return QueryResultHelper.ToDictionary(this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            if (Data is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (_disposable is not null)
            {
                _disposable.Dispose();
            }

            _disposed = true;
        }
    }
}
