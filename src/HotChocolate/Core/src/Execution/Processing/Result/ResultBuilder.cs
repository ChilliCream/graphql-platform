using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution.Properties;

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
    private readonly object _syncErrors = new();
    private readonly List<IError> _errors = new();
    private readonly HashSet<ISelection> _fieldErrors = new();
    private readonly List<NonNullViolation> _nonNullViolations = new();
    private readonly HashSet<uint> _removedResults = new();
    private readonly HashSet<uint> _patchIds = new();

    private readonly object _syncExtensions = new();
    private readonly Dictionary<string, object?> _extensions = new();
    private readonly Dictionary<string, object?> _contextData = new();
    private readonly List<Func<ValueTask>> _cleanupTasks = new();

    private ResultMemoryOwner _resultOwner = default!;
    private ObjectResult? _data;
    private Path? _path;
    private string? _label;
    private bool? _hasNext;

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

    /// <summary>
    /// Register cleanup tasks that will be executed after resolver execution is finished.
    /// </summary>
    /// <param name="action">
    /// Cleanup action.
    /// </param>
    public void RegisterForCleanup(Func<ValueTask> action)
    {
        lock (_syncExtensions)
        {
            _cleanupTasks.Add(action);
        }
    }

    public void RegisterForCleanup<T>(T state, Func<T, ValueTask> action)
    {
        lock (_syncExtensions)
        {
            _cleanupTasks.Add(() => action(state));
        }
    }

    public void SetPath(Path? path)
        => _path = path;

    public void SetLabel(string? label)
        => _label = label;

    public void SetHasNext(bool value)
        => _hasNext = value;

    public void AddError(IError error, ISelection? selection = null)
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

    public void AddNonNullViolation(ISelection selection, Path path, ObjectResult parent)
    {
        lock (_syncErrors)
        {
            _nonNullViolations.Add(new NonNullViolation(selection, path, parent));
        }
    }

    public void AddRemovedResult(ResultData result)
    {
        lock (_syncErrors)
        {
            if (result.PatchId > 0)
            {
                _removedResults.Add(result.PatchId);
            }
        }
    }

    public void AddPatchId(uint patchId)
    {
        lock (_syncExtensions)
        {
            _patchIds.Add(patchId);
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

        if (_errors.Count > 0)
        {
            _errors.Sort(ErrorComparer.Default);
        }

        _removedResults.Remove(0);
        if (_removedResults.Count > 0)
        {
            _contextData.Add(WellKnownContextData.RemovedResults, _removedResults.ToArray());
        }

        _patchIds.Remove(0);
        if (_patchIds.Count > 0)
        {
            _contextData.Add(WellKnownContextData.ExpectedPatches, _patchIds.ToArray());
        }

        var result = new QueryResult
        (
            _data,
            _errors.Count == 0 ? null : new List<IError>(_errors),
            CreateExtensionData(_extensions),
            CreateExtensionData(_contextData),
            null,
            _label,
            _path,
            _hasNext,
            _cleanupTasks.Count > 0
                ? _cleanupTasks.ToArray()
                : Array.Empty<Func<ValueTask>>()
        );

        if (_data is not null)
        {
            result.RegisterForCleanup(_resultOwner);
        }

        return result;
    }

    public IQueryResultBuilder BuildResultBuilder()
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

        var builder = QueryResultBuilder.New();

        builder.SetData(_data);

        if (_errors.Count > 0)
        {
            _errors.Sort(ErrorComparer.Default);
            builder.AddErrors(_errors);
        }

        _removedResults.Remove(0);
        if (_removedResults.Count > 0)
        {
            _contextData.Add(WellKnownContextData.RemovedResults, _removedResults.ToArray());
        }

        builder.SetExtensions(CreateExtensionData(_extensions));
        builder.SetContextData(CreateExtensionData(_contextData));
        builder.SetLabel(_label);
        builder.SetPath(_path);

        if (_data is not null)
        {
            builder.RegisterForCleanup(_resultOwner);
        }

        return builder;
    }

    private static IReadOnlyDictionary<string, object?>? CreateExtensionData(
        Dictionary<string, object?> data)
    {
        if (data.Count == 0)
        {
            return null;
        }

        return ImmutableDictionary.CreateRange(data);
    }

    public void DiscardResult()
        => _resultOwner.Dispose();

    private sealed class ErrorComparer : IComparer<IError>
    {
        public int Compare(IError? x, IError? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (ReferenceEquals(null, y))
            {
                return 1;
            }

            if (ReferenceEquals(null, x))
            {
                return -1;
            }

            if (y.Locations?.Count > 0)
            {
                if (x.Locations?.Count > 0)
                {
                    return x.Locations[0].CompareTo(y.Locations[0]);
                }
                return 1;
            }

            if (x.Locations?.Count > 0)
            {
                return -1;
            }

            return 0;
        }

        public static readonly ErrorComparer Default = new();
    }
}
