using System.Collections.Concurrent;
using System.Collections.Immutable;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution;

public sealed class FetchResultStore
{
    private readonly ConcurrentDictionary<Path, List<FetchResult>> _results = new();
    private readonly ConcurrentDictionary<SelectionPath, List<FetchResult>> _resultsBySelectionPath = new();
    private readonly ConcurrentDictionary<int, List<FetchResult>> _resultsByExecutionNodeId = new();
    private ImmutableHashSet<SelectionPath> _selectionPaths = [];

    public void AddResult(FetchResult result)
    {
        var results = _results.GetOrAdd(result.Path, _ => []);

        lock (results)
        {
            results.Add(result);
        }

        var resultsBySelectionPath = _resultsBySelectionPath.GetOrAdd(result.Target, _ => []);

        lock (resultsBySelectionPath)
        {
            resultsBySelectionPath.Add(result);
        }

        var resultsByExecutionNodeId = _resultsByExecutionNodeId.GetOrAdd(result.ExecutionNodeId, _ => []);

        lock (resultsByExecutionNodeId)
        {
            resultsByExecutionNodeId.Add(result);
        }

        lock (_selectionPaths)
        {
            _selectionPaths = _selectionPaths.Add(result.Target);
        }
    }

    public IReadOnlyList<FetchResult> GetResults(SelectionPath path)
    {
        List<FetchResult>? results = null;

        foreach (var selectionPath in _selectionPaths)
        {
            if (selectionPath.IsParentOfOrSame(path))
            {
                results ??= [];
                results.AddRange(_resultsBySelectionPath[selectionPath]);
            }
        }

        return results ?? [];
    }

    public IReadOnlyList<FetchResult> GetResults(IEnumerable<int> executionNodeIds)
    {
        List<FetchResult>? results = null;

        foreach (var executionNodeId in executionNodeIds)
        {
            if (_resultsByExecutionNodeId.TryGetValue(executionNodeId, out var fetchResults))
            {
                results ??= [];
                results.AddRange(fetchResults);
            }
        }

        return results ?? [];
    }
}
