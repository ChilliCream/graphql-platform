using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotChocolate.Execution;

public sealed class QueryResultBuilder : IQueryResultBuilder
{
    private IReadOnlyDictionary<string, object?>? _data;
    private IReadOnlyList<object?>? _items;
    private List<IError>? _errors;
    private ExtensionData? _extensionData;
    private ExtensionData? _contextData;
    private List<IQueryResult>? _incremental;
    private string? _label;
    private Path? _path;
    private bool? _hasNext;
    private bool? _isDataSet;
    private Func<ValueTask>[] _cleanupTasks = Array.Empty<Func<ValueTask>>();

    public IQueryResultBuilder SetData(IReadOnlyDictionary<string, object?>? data)
    {
        _data = data;
        _items = null;
        _isDataSet = true;
        return this;
    }

    public IQueryResultBuilder SetItems(IReadOnlyList<object?>? items)
    {
        _items = items;

        if (items is not null)
        {
            _data = null;
        }

        return this;
    }

    public IQueryResultBuilder AddError(IError error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        _errors ??= [];
        _errors.Add(error);
        return this;
    }

    public IQueryResultBuilder AddErrors(IEnumerable<IError> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        _errors ??= [];
        _errors.AddRange(errors);
        return this;
    }

    public IQueryResultBuilder AddExtension(string key, object? data)
    {
        _extensionData ??= new ExtensionData();
        _extensionData.Add(key, data);
        return this;
    }

    public IQueryResultBuilder SetExtension(string key, object? data)
    {
        _extensionData ??= new ExtensionData();
        _extensionData[key] = data;
        return this;
    }

    public IQueryResultBuilder SetExtensions(IReadOnlyDictionary<string, object?>? extensions)
    {
        if (extensions is ExtensionData extensionData)
        {
            _extensionData = extensionData;
        }
        else if (extensions is not null)
        {
            _extensionData = new ExtensionData(extensions);
        }
        else
        {
            _extensionData = null;
        }
        return this;
    }

    public IQueryResultBuilder AddContextData(string key, object? data)
    {
        _contextData ??= new ExtensionData();
        _contextData.Add(key, data);
        return this;
    }

    public IQueryResultBuilder SetContextData(string key, object? data)
    {
        _contextData ??= new ExtensionData();
        _contextData[key] = data;
        return this;
    }

    public IQueryResultBuilder SetContextData(IReadOnlyDictionary<string, object?>? contextData)
    {
        if (contextData is ExtensionData extensionData)
        {
            _contextData = extensionData;
        }
        else if (contextData is not null)
        {
            _contextData = new ExtensionData(contextData);
        }
        else
        {
            _contextData = null;
        }
        return this;
    }

    public IQueryResultBuilder AddPatch(IQueryResult patch)
    {
        if (patch is null)
        {
            throw new ArgumentNullException(nameof(patch));
        }

        _incremental ??= [];
        _incremental.Add(patch);
        return this;
    }

    public IQueryResultBuilder SetLabel(string? label)
    {
        _label = label;
        return this;
    }

    public IQueryResultBuilder SetPath(Path? path)
    {
        _path = path;
        return this;
    }

    public IQueryResultBuilder SetHasNext(bool? hasNext)
    {
        _hasNext = hasNext;
        return this;
    }

    public IQueryResultBuilder RegisterForCleanup(Func<ValueTask> clean)
    {
        if (clean is null)
        {
            throw new ArgumentNullException(nameof(clean));
        }

        var index = _cleanupTasks.Length;
        Array.Resize(ref _cleanupTasks, index + 1);
        _cleanupTasks[index] = clean;
        return this;
    }

    public IQueryResult Create()
        => new QueryResult(
            _data,
            _errors?.Count > 0 ? _errors : null,
            _extensionData?.Count > 0 ? _extensionData : null,
            _contextData?.Count > 0 ? _contextData : null,
            _items,
            _incremental,
            _label,
            _path,
            _hasNext,
            _cleanupTasks,
            _isDataSet ?? false);

    public static QueryResultBuilder New() => new();

    public static QueryResultBuilder FromResult(IQueryResult result)
    {
        var builder = new QueryResultBuilder { _data = result.Data, };

        if (result.Errors is not null)
        {
            builder._errors = [..result.Errors,];
        }

        if (result.Extensions is ExtensionData ext)
        {
            builder._extensionData = new ExtensionData(ext);
        }
        else if (result.Extensions is not null)
        {
            builder._extensionData = new ExtensionData(result.Extensions);
        }

        if (result.ContextData is ExtensionData cd)
        {
            builder._contextData = new ExtensionData(cd);
        }
        else if (result.ContextData is not null)
        {
            builder._contextData = new ExtensionData(result.ContextData);
        }

        builder._label = result.Label;
        builder._path = result.Path;
        builder._hasNext = result.HasNext;
        builder._isDataSet = result.IsDataSet;

        return builder;
    }

    public static IQueryResult CreateError(
        IError error,
        IReadOnlyDictionary<string, object?>? contextData = null)
        => error is AggregateError aggregateError
            ? CreateError(aggregateError.Errors, contextData)
            : new QueryResult(null, new List<IError> { error, }, contextData: contextData);

    public static IQueryResult CreateError(
        IReadOnlyList<IError> errors,
        IReadOnlyDictionary<string, object?>? contextData = null)
        => new QueryResult(null, errors, contextData: contextData);
}
