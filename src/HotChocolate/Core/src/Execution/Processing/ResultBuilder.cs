using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution.Properties;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
    private readonly object _syncErrors = new();
    private readonly List<IError> _errors = new();
    private readonly HashSet<FieldNode> _fieldErrors = new();
    private readonly List<NonNullViolation> _nonNullViolations = new();

    private readonly object _syncExtensions = new();
    private readonly Dictionary<string, object?> _extensions = new();
    private readonly Dictionary<string, object?> _contextData = new();

    private ResultMemoryOwner _resultOwner;
    private ObjectResult? _data;
    private Path? _path;
    private string? _label;
    private bool? _hasNext;

    public ResultBuilder(ResultPool resultPool)
    {
        _resultPool = resultPool;
        _resultOwner = new ResultMemoryOwner(resultPool);
    }

    public IReadOnlyList<IError> Errors => _errors;

    public void SetData(ObjectResult data)
        => _data = data;

    public void SetExtension(string key, object? value)
    {
        lock (_syncExtensions)
        {
            _extensions[key] = value;
        }
    }

    public void SetContextData(string key, object? value)
    {
        lock (_syncExtensions)
        {
            _contextData[key] = value;
        }
    }

    public void SetPath(Path? path)
        => _path = path;

    public void SetLabel(string? label)
        => _label = label;

    public void SetHasNext(bool value)
        => _hasNext = value;

    public void AddError(IError error, FieldNode? selection = null)
    {
        lock (_syncErrors)
        {
            _errors.Add(error);
            if (selection is { })
            {
                _fieldErrors.Add(selection);
            }
        }
    }

    public void AddErrors(IEnumerable<IError> errors, FieldNode? selection = null)
    {
        lock (_syncErrors)
        {
            _errors.AddRange(errors);

            if (selection is { })
            {
                _fieldErrors.Add(selection);
            }
        }
    }

    public void AddNonNullViolation(FieldNode selection, Path path, ObjectResult parent)
    {
        lock (_syncErrors)
        {
            _nonNullViolations.Add(new NonNullViolation(selection, path, parent));
        }
    }

    public IQueryResult BuildResult()
    {
        if (!ApplyNonNullViolations(_errors, _nonNullViolations, _fieldErrors))
        {
            // The non-null violation cased the whole result being deleted.
            _data = null;
            _resultOwner.Dispose();
        }

        if (_data is null && _errors.Count == 0 && _hasNext is not false)
        {
            throw new InvalidOperationException(
                Resources.ResultHelper_BuildResult_InvalidResult);
        }

        var result = new QueryResult
        (
            _data,
            _errors.Count == 0 ? null : new List<IError>(_errors),
            CreateExtensionData(_extensions),
            CreateExtensionData(_contextData),
            _label,
            _path,
            _hasNext
        );

        if (_data is not null)
        {
            result.RegisterForCleanup(_resultOwner);
        }

        return result;
    }

    private IReadOnlyDictionary<string, object?>? CreateExtensionData(
        Dictionary<string, object?> data)
    {
        if (data.Count == 0)
        {
            return null;
        }

        if (data.Count == 1)
        {
            var value = data.Single();
            return new SingleValueExtensionData(value.Key, value.Value);
        }

        return ImmutableDictionary.CreateRange(data);
    }

    public void DropResult() => _resultOwner.Dispose();
}


