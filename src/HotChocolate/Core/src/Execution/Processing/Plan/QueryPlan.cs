using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Execution.Properties;

namespace HotChocolate.Execution.Processing.Plan;

internal sealed class QueryPlan
{
    private readonly List<ExecutionStep> _steps = new();
    private readonly Dictionary<int, ExecutionStep> _stepBySelectionId = new();
    private readonly QueryPlan[] _deferredPlans;
    private readonly Dictionary<int, QueryPlan>? _streamPlans;

    public QueryPlan(
        ExecutionStep root,
        QueryPlan[]? deferredPlans = null,
        Dictionary<int, QueryPlan>? streamPlans = null)
    {
        Root = root;
        _deferredPlans = deferredPlans ?? Array.Empty<QueryPlan>();
        _streamPlans = streamPlans;

        var count = 0;
        AssignId(root, ref count);
        Count = count;
    }

    public ExecutionStep Root { get; }

    public int Count { get; }

    public QueryPlan GetDeferredPlan(int fragmentId)
    {
        if (fragmentId >= _deferredPlans.Length)
        {
            throw new ArgumentException(
                Resources.QueryPlan_InvalidFragmentId,
                nameof(fragmentId));
        }

        return _deferredPlans[fragmentId];
    }

    public QueryPlan GetStreamPlan(int selectionId)
    {
        if (_streamPlans is null)
        {
            throw new NotSupportedException("This query plan has no streams.");
        }

        return _streamPlans[selectionId];
    }

    internal bool TryGetStep(IExecutionTask task, [MaybeNullWhen(false)] out ExecutionStep step)
    {
        if (task.State is ExecutionStep ts1)
        {
            step = ts1;
            return true;
        }

        if (task is ResolverTask resolverTask &&
            _stepBySelectionId.TryGetValue(resolverTask.Selection.Id, out step))
        {
            return true;
        }

        foreach (ExecutionStep ts2 in _steps)
        {
            if (ts2.IsOwningTask(task))
            {
                step = ts2;
                return true;
            }
        }

        step = null;
        return false;
    }

    internal bool TryGetStep(int stepId, [MaybeNullWhen(false)] out ExecutionStep step)
    {
        if (stepId < _steps.Count)
        {
            step = _steps[stepId];
            return true;
        }

        step = null;
        return false;
    }

    private void AssignId(ExecutionStep current, ref int stepId)
    {
        current.Id = stepId++;
        _steps.Add(current);

        if (current is ResolverStep resolverStep)
        {
            foreach (ISelection selection in resolverStep.Selections)
            {
                _stepBySelectionId[selection.Id] = current;
            }
        }

        foreach (ExecutionStep? step in current.Steps)
        {
            AssignId(step, ref stepId);
        }
    }
}
