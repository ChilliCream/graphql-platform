using System.Collections.Concurrent;
using HotChocolate.Text.Json;

namespace HotChocolate.Execution.Processing;

internal sealed class DeferExecutionCoordinator
{
    private const int InitialResultId = -1;
    private readonly object _sync = new();
    private readonly ConcurrentDictionary<DeferredResultInfo, int> _resultIds = new();
    private readonly ConcurrentDictionary<int, DeferredResultInfo> _resultInfoLookup = new();
    private readonly ConcurrentDictionary<ResultDocument, HashSet<int>> _branches = new();
    private readonly ConcurrentDictionary<int, ResultDocument> _completed = new();
    private int _nextId;

    public int Branch(ResultDocument parent, Path path, DeferUsage deferUsage)
    {
        var resultInfo = new DeferredResultInfo(path, deferUsage);

        if (!_resultIds.TryGetValue(resultInfo, out var resultId))
        {
            lock (_sync)
            {
                if (!_resultIds.TryGetValue(resultInfo, out resultId))
                {
                    resultId = _nextId++;
                    GetBranchesUnsafe(parent).Add(resultId);
                    _resultInfoLookup.TryAdd(resultId, resultInfo);
                    _resultIds.TryAdd(resultInfo, resultId);
                }
            }
        }

        return resultId;
    }

    public void EnqueueResult(ResultDocument result)
        => _completed.TryAdd(InitialResultId, result);

    public void EnqueueResult(ResultDocument result, int resultId)
        => _completed.TryAdd(resultId, result);

    private HashSet<int> GetBranchesUnsafe(ResultDocument result)
    {
        if (!_branches.TryGetValue(result, out var branches))
        {
            branches = [];
            _branches.TryAdd(result, branches);
        }

        return branches;
    }

    private readonly record struct DeferredResultInfo(Path Path, DeferUsage Group);
}

