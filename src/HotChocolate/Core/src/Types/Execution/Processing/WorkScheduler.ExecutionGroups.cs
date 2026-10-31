using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing;

internal sealed partial class WorkScheduler
{
    private ExecutionGroupScope? _groupScopes;
    private volatile bool _hasExecutionGroups;

    /// <summary>
    /// Opens an execution group scope that stays valid until the scheduler is cleared, or returns
    /// <c>null</c> if the group has nothing to sequence.
    /// </summary>
    public ExecutionGroupScope? OpenExecutionGroupScope(ExecutionGroupPlan plan, ConditionFlags includeFlags)
    {
        AssertNotPooled();

        var scope = ExecutionGroupScope.Open(this, plan, includeFlags);

        if (scope is null)
        {
            return null;
        }

        _hasExecutionGroups = true;

        ExecutionGroupScope? head;

        do
        {
            head = Volatile.Read(ref _groupScopes);
            scope.NextScope = head;
        }
        while (Interlocked.CompareExchange(ref _groupScopes, scope, head) != head);

        return scope;
    }

    /// <summary>
    /// Registers a task of an execution group member that was held back by its scope.
    /// </summary>
    public void RegisterReserved(ResolverTask task)
    {
        AssertNotPooled();

        var work = task.IsSerial ? _serial : _work;
        task.IsRegistered = true;
        task.Id = Interlocked.Increment(ref _nextId);

        lock (_sync)
        {
            work.Push(task);
        }

        _signal.Set();
    }

    private void Reserve(ResolverTask task)
    {
        AssertNotPooled();

        lock (_sync)
        {
            RegisterBranchTaskUnsafe(task.BranchId);
            IncrementPathCountUnsafe(task.Selection.FieldSelectionPath);
        }
    }

    private void RegisterMember(ResolverTask member)
    {
        var scope = member.GroupScope!;

        if (scope.CanRun(member.GroupSlot))
        {
            RegisterCore(member);
            return;
        }

        Reserve(member);
        scope.Enqueue(member);
    }

    private static bool ContainsMember(ReadOnlySpan<IExecutionTask> tasks)
    {
        foreach (var task in tasks)
        {
            if (task is ResolverTask { IsGroupMember: true })
            {
                return true;
            }
        }

        return false;
    }

    private void RegisterWithMembers(ReadOnlySpan<IExecutionTask> tasks)
    {
        for (var i = tasks.Length - 1; i >= 0; i--)
        {
            if (tasks[i] is ResolverTask { IsGroupMember: true } member)
            {
                RegisterMember(member);
            }
            else
            {
                RegisterCore(tasks[i]);
            }
        }
    }

    private void ReleaseExecutionGroupScopes()
    {
        var scope = Interlocked.Exchange(ref _groupScopes, null);
        var reuse = !_ct.IsCancellationRequested;

        // after an aborted request tasks can still be running, so their scopes are not handed out again.
        while (scope is not null)
        {
            var next = scope.NextScope;

            if (reuse)
            {
                scope.Return();
            }

            scope = next;
        }
    }
}
