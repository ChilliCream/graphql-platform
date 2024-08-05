using System.Runtime.CompilerServices;
using HotChocolate.Execution.Properties;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
    private static readonly Func<ValueTask>[] _emptyCleanupTasks = [];
    private readonly List<IError> _errors = [];
    private readonly HashSet<Path> _errorPaths = [];
    private readonly HashSet<ISelection> _fieldErrors = [];
    private readonly List<NonNullViolation> _nonNullViolations = [];
    private readonly HashSet<uint> _removedResults = [];
    private readonly HashSet<uint> _patchIds = [];

    private readonly Dictionary<string, object?> _extensions = new();
    private readonly Dictionary<string, object?> _contextData = new();
    private readonly List<Func<ValueTask>> _cleanupTasks = [];

    private ResultMemoryOwner _resultOwner = default!;
    private ObjectResult? _data;
    private IReadOnlyList<object?>? _items;
    private Path? _path;
    private string? _label;
    private bool? _hasNext;
    private int? _requestIndex;
    private int? _variableIndex;
    private bool _singleErrorPerPath;

    public IReadOnlyList<IError> Errors => _errors;

    public void SetData(ObjectResult? data)
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

    public void RegisterForCleanup<T>(T state) where T : IDisposable
    {
        lock (_cleanupTasks)
        {
            _cleanupTasks.Add(
                () =>
                {
                    state.Dispose();
                    return default!;
                });
        }
    }

    public void SetPath(Path? path)
        => _path = path;

    public void SetLabel(string? label)
        => _label = label;

    public void SetHasNext(bool value)
        => _hasNext = value;

    public void SetSingleErrorPerPath(bool value = true)
    {
        _singleErrorPerPath = value;
    }

    public void AddError(IError error, ISelection? selection = null)
    {
        lock (_errors)
        {
            if (!_singleErrorPerPath || error.Path is null || _errorPaths.Add(error.Path))
            {
                _errors.Add(error);
            }

            if (selection is not null)
            {
                _fieldErrors.Add(selection);
            }
        }
    }

    public void AddNonNullViolation(ISelection selection, Path path)
    {
        var violation = new NonNullViolation(selection, path);

        lock (_errors)
        {
            _nonNullViolations.Add(violation);
        }
    }

    public void AddRemovedResult(ResultData result)
    {
        if (result.PatchId <= 0)
        {
            return;
        }

        lock (_errors)
        {
            _removedResults.Add(result.PatchId);
        }
    }

    public void AddPatchId(uint patchId)
    {
        lock (_patchIds)
        {
            _patchIds.Add(patchId);
        }
    }

    public void SetRequestIndex(int requestIndex)
        => _requestIndex = requestIndex;

    public void SetVariableIndex(int variableIndex)
        => _variableIndex = variableIndex;

    // ReSharper disable InconsistentlySynchronizedField
    public IOperationResult BuildResult()
    {
        ApplyNonNullViolations(_errors, _nonNullViolations, _fieldErrors);

        if (_data?.IsInvalidated == true)
        {
            // The non-null violation cased the whole result being deleted.
            _data = null;
            _resultOwner.Dispose();
        }

        if (_data is null && _items is null && _errors.Count == 0 && _hasNext is not false)
        {
            throw new InvalidOperationException(Resources.ResultHelper_BuildResult_InvalidResult);
        }

        if (_errors.Count > 1)
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

        var result = new OperationResult(
            _data,
            _errors.Count == 0 ? null : _errors.ToArray(),
            CreateExtensionData(_extensions),
            CreateExtensionData(_contextData),
            incremental: null,
            items: _items,
            label: _label,
            path: _path,
            hasNext: _hasNext,
            cleanupTasks: _cleanupTasks.Count == 0
                ? _emptyCleanupTasks
                : _cleanupTasks.ToArray(),
            isDataSet: true,
            requestIndex: _requestIndex,
            variableIndex: _variableIndex);

        if (_data is not null)
        {
            result.RegisterForCleanup(_resultOwner);
        }

        return result;
    }

    // ReSharper restore InconsistentlySynchronizedField

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Dictionary<string, object?>? CreateExtensionData(Dictionary<string, object?> data)
        => data.Count == 0 ? null : new Dictionary<string, object?>(data);

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
