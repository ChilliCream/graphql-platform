using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Execution.Processing;

internal sealed class DeferredWorkState
{
    private readonly int _deliverableQueueSize;

    private readonly object _completeSync = new();
    private readonly object _deliverSync = new();
    private readonly object _patchSync = new();

    private readonly List<DeferredExecutionTaskResult> _ready = [];
    private readonly Queue<IQueryResult> _deliverable = new();
    private readonly HashSet<uint> _completed = [];
    private readonly HashSet<uint> _notPatchable = [];
    private SemaphoreSlim _dequeueSemaphore = new(0);
    private SemaphoreSlim _enqueueSemaphore;
    private uint _taskId;
    private uint _work;
    private uint _patchId;

    public DeferredWorkState(int bufferSize)
    {
        _deliverableQueueSize = bufferSize;
        _enqueueSemaphore = new(_deliverableQueueSize);
    }

    public bool HasResults => _taskId > 0;

    public bool IsCompleted => _work is 0;

    public uint CreateId()
    {
        lock (_deliverSync)
        {
            _work++;
            return ++_taskId;
        }
    }

    public uint AssignPatchId(ResultData resultData)
    {
        if (resultData.PatchId == 0)
        {
            lock (_patchSync)
            {
                if (resultData.PatchId == 0)
                {
                    var patchId = ++_patchId;
                    resultData.PatchId = patchId;
                    return patchId;
                }
            }
        }

        return resultData.PatchId;
    }

    public void Complete(DeferredExecutionTaskResult result, CancellationToken cancellationToken)
    {
        var update = true;

        try
        {
            lock (_completeSync)
            {
                if (result.ParentTaskId is 0 || _completed.Contains(result.ParentTaskId))
                {
                    _completed.Add(result.TaskId);
                    EnqueueResult(result.Result, cancellationToken);

                    var evaluateDeferredResults = _ready.Count > 0;

                    while (evaluateDeferredResults)
                    {
                        var i = 0;
                        evaluateDeferredResults = false;

                        while (_ready.Count > 0 && i < _ready.Count)
                        {
                            var current = _ready[i];

                            if (_completed.Contains(current.ParentTaskId))
                            {
                                _completed.Add(current.TaskId);
                                _ready.RemoveAt(i);
                                EnqueueResult(current.Result, cancellationToken);
                                evaluateDeferredResults = true;
                            }
                            else
                            {
                                i++;
                            }
                        }
                    }
                }
                else
                {
                    _ready.Add(result);
                    update = false;
                }
            }
        }
        finally
        {
            if (update)
            {
                _dequeueSemaphore.Release();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnqueueResult(IQueryResult? queryResult, CancellationToken cancellationToken)
    {
        _enqueueSemaphore.Wait(cancellationToken);

        lock (_deliverSync)
        {
            if (queryResult is not null)
            {
                _deliverable.Enqueue(queryResult);
            }
            else
            {
                _work--;
            }
        }
    }

    public async ValueTask<IQueryResult?> TryDequeueResultsAsync(
        CancellationToken cancellationToken)
    {
        await _dequeueSemaphore.WaitAsync(cancellationToken);

        lock (_deliverSync)
        {
            if (_deliverable.Count > 0)
            {
                var hasNext = true;
                var result = new IQueryResult[_deliverable.Count];
                var consumed = 0;

                for (var i = 0; i < result.Length; i++)
                {
                    var deliverable = _deliverable.Dequeue();
                    _enqueueSemaphore.Release();

                    if (--_work is 0)
                    {
                        _dequeueSemaphore.Release();
                        hasNext = false;
                    }

                    // if the deferred result can still be patched into the result set from which
                    // it was being spawned of we will add it to the result batch.
                    if ((deliverable.ContextData?.TryGetValue(PatchId, out var value) ?? false) &&
                        value is uint patchId &&
                        !_notPatchable.Contains(patchId))
                    {
                        AddRemovedResultSetsToNotPatchable(deliverable, _notPatchable);
                        result[consumed++] = deliverable;
                    }

                    // if the item is not patchable we will discard it and mark all dependant
                    // results as not patchable.
                    else
                    {
                        AddAllResultSetsToNotPatchable(deliverable, _notPatchable);
                    }
                }

                if (consumed < result.Length)
                {
                    Array.Resize(ref result, consumed);
                }

                return new QueryResult(null, incremental: result, hasNext: hasNext);
            }
        }

        return null;

        static void AddRemovedResultSetsToNotPatchable(
            IQueryResult result,
            HashSet<uint> notPatchable)
        {
            if ((result.ContextData?.TryGetValue(RemovedResults, out var value) ?? false) &&
                value is IEnumerable<uint> patchIds)
            {
                foreach (var patchId in patchIds)
                {
                    notPatchable.Add(patchId);
                }
            }
        }

        static void AddAllResultSetsToNotPatchable(
            IQueryResult result,
            HashSet<uint> notPatchable)
        {
            if ((result.ContextData?.TryGetValue(ExpectedPatches, out var value) ?? false) &&
                value is IEnumerable<uint> patchIds)
            {
                foreach (var patchId in patchIds)
                {
                    notPatchable.Add(patchId);
                }
            }
        }
    }

    public void Reset()
    {
        _dequeueSemaphore.Dispose();
        _dequeueSemaphore = new SemaphoreSlim(0);
        _ready.Clear();
        _completed.Clear();
        _deliverable.Clear();
        _enqueueSemaphore.Dispose();
        _enqueueSemaphore = new SemaphoreSlim(_deliverableQueueSize);
        _notPatchable.Clear();
        _taskId = 0;
        _work = 0;
        _patchId = 0;
    }
}
