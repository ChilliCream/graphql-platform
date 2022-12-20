using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HotChocolate.Execution.Properties;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
    private readonly List<IError> _errors = new();
    private readonly HashSet<ISelection> _fieldErrors = new();
    private readonly List<NonNullViolation> _nonNullViolations = new();
    private readonly HashSet<uint> _removedResults = new();
    private readonly HashSet<uint> _patchIds = new();

    private readonly Dictionary<string, object?> _extensions = new();
    private readonly Dictionary<string, object?> _contextData = new();
    private readonly List<Func<ValueTask>> _cleanupTasks = new();

    private ResultMemoryOwner _resultOwner = default!;
    private ObjectResult? _data;
    private IReadOnlyList<object?>? _items;
    private Path? _path;
    private string? _label;
    private bool? _hasNext;

    public IReadOnlyList<IError> Errors => _errors;

    public void SetData(ObjectResult data)
    {
        if (_items is not null)
        {
            throw new InvalidOperationException(
                Resources.ResultBuilder_DataAndItemsNotAllowed);
        }

        _data = data;
    }

    public void SetItems(IReadOnlyList<object?> items)
    {
        if (_data is not null)
        {
            throw new InvalidOperationException(
                Resources.ResultBuilder_DataAndItemsNotAllowed);
        }

        _items = items;
    }

    public void SetExtension(string key, object? value)
    {
        lock (_extensions)
        {
            _extensions[key] = value;
        }
    }

    public void SetExtension<T>(string key, UpdateState<T> value)
    {
        lock (_extensions)
        {
            if (_extensions.TryGetValue(key, out var current) && current is T casted)
            {
                _extensions[key] = value(key, casted);
            }
            else
            {
                _extensions[key] = value(key, default!);
            }
        }
    }

    public void SetExtension<T, TState>(string key, TState state, UpdateState<T, TState> value)
    {
        lock (_extensions)
        {
            if (_extensions.TryGetValue(key, out var current) && current is T casted)
            {
                _extensions[key] = value(key, casted, state);
            }
            else
            {
                _extensions[key] = value(key, default!, state);
            }
        }
    }

    public void SetContextData(string key, object? value)
    {
        lock (_contextData)
        {
            _contextData[key] = value;
        }
    }

    public void SetContextData(string key, UpdateState<object?> value)
    {
        lock (_contextData)
        {
            _contextData.TryGetValue(key, out var current);
            _contextData[key] = value(key, current);
        }
    }

    public void SetContextData<TState>(string key, TState state, UpdateState<object?, TState> value)
    {
        lock (_contextData)
        {
            _contextData.TryGetValue(key, out var current);
            _contextData[key] = value(key, current, state);
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
        lock (_cleanupTasks)
        {
            _cleanupTasks.Add(action);
        }
    }

    public void RegisterForCleanup<T>(T state, Func<T, ValueTask> action)
    {
        lock (_cleanupTasks)
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
        lock (_errors)
        {
            _errors.Add(error);
            if (selection is not null)
            {
                _fieldErrors.Add(selection);
            }
        }
    }

    public void AddNonNullViolation(ISelection selection, Path path, ObjectResult parent)
    {
        var violation = new NonNullViolation(selection, path.Clone(), parent);

        lock (_errors)
        {
            _nonNullViolations.Add(violation);
        }
    }

    public void AddRemovedResult(ResultData result)
    {
        lock (_errors)
        {
            if (result.PatchId > 0)
            {
                _removedResults.Add(result.PatchId);
            }
        }
    }

    public void AddPatchId(uint patchId)
    {
        lock (_patchIds)
        {
            _patchIds.Add(patchId);
        }
    }

    // ReSharper disable InconsistentlySynchronizedField
    //
    public IQueryResult BuildResult()
    {
        if (!ApplyNonNullViolations(_errors, _nonNullViolations, _fieldErrors))
        {
            // The non-null violation cased the whole result being deleted.
            _data = null;
            _resultOwner.Dispose();
        }

        if (_data is null && _items is null && _errors.Count == 0 && _hasNext is not false)
        {
            throw new InvalidOperationException(Resources.ResultHelper_BuildResult_InvalidResult);
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

        var result = new QueryResult(
            _data,
            _errors.Count == 0
                ? null
                : new List<IError>(_errors),
            CreateExtensionData(_extensions),
            CreateExtensionData(_contextData),
            incremental: null,
            items: _items,
            label: _label,
            path: _path,
            hasNext: _hasNext,
            cleanupTasks: _cleanupTasks.Count > 0
                ? _cleanupTasks.ToArray()
                : Array.Empty<Func<ValueTask>>()
        );

        if (_data is not null)
        {
            result.RegisterForCleanup(_resultOwner);
        }

        return result;
    }
    // ReSharper restore InconsistentlySynchronizedField

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ImmutableDictionary<string, object?>? CreateExtensionData(
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
