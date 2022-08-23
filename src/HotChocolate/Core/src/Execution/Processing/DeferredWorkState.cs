using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing;

internal sealed class DeferredWorkState
{
    private readonly object _completeSync = new();
    private readonly object _deliverSync = new();

    private readonly List<DeferredExecutionTaskResult> _ready = new();
    private readonly Queue<IQueryResultBuilder> _deliverable = new();
    private readonly HashSet<uint> _completed = new();
    private SemaphoreSlim _semaphore = new(0);
    private uint _taskId;
    private uint _delivered;

    public bool HasResults => _taskId > 0;

    public uint CreateId()
    {
        lock (_deliverSync)
        {
            return ++_taskId;
        }
    }

    public void Complete(DeferredExecutionTaskResult result)
    {
        lock (_completeSync)
        {
            if (result.ParentTaskId is 0 || _completed.Contains(result.ParentTaskId))
            {
                _completed.Add(result.TaskId);
                EnqueueResult(result.Result);

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
                            EnqueueResult(current.Result);
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
            }
        }
    }

    private void EnqueueResult(IQueryResultBuilder? queryResult)
    {
        if (queryResult is not null)
        {
            lock (_deliverSync)
            {
                _deliverable.Enqueue(queryResult);
            }
        }
        _semaphore.Release();
    }

    public async ValueTask<IQueryResult?> TryDequeueResultAsync(
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        lock (_deliverSync)
        {
            while (_deliverable.Count > 0)
            {
#if NETSTANDARD2_0
                if (_deliverable.Count > 0)
                {
                    var result = _deliverable.Dequeue();
#else
                if (_deliverable.TryDequeue(out var result))
                {
#endif
                    if (++_delivered == _taskId)
                    {
                        _semaphore.Release();
                        result.SetHasNext(false);
                    }
                    else
                    {
                        result.SetHasNext(true);
                    }

                    return result.Create();
                }

                if (++_delivered == _taskId)
                {
                    return new QueryResult(null, hasNext: false);
                }
            }
        }

        return null;
    }

    public void Reset()
    {
        _semaphore = new SemaphoreSlim(0);
        _ready.Clear();
        _completed.Clear();
        _deliverable.Clear();
        _taskId = 0;
        _delivered = 0;
    }
}
