using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Execution;

internal sealed class ExecutionState : IExecutionState
{
    private readonly Dictionary<ISelectionSet, List<WorkItem>> _map = new();
    private readonly HashSet<ISelectionSet> _immutable = new();

    public bool ContainsState(ISelectionSet selectionSet)
    {
        var taken = false;
        Monitor.Enter(_map, ref taken);

        try
        {
            return _map.ContainsKey(selectionSet);
        }
        finally
        {
            if (taken)
            {
                Monitor.Exit(_map);
            }
        }
    }

    public bool TryGetState(
        ISelectionSet selectionSet,
        [NotNullWhen(true)] out IReadOnlyList<WorkItem>? values)
    {
        var taken = false;
        Monitor.Enter(_map, ref taken);

        try
        {
            // We mark a value immutable on first read.
            //
            // After we accessed the first time the state of a selection set its no longer allowed
            // to mutate it.
            //
            // The query plan should actually be ordered in a way that there are no mutations after
            // the state is being read from nodes.
            _immutable.Add(selectionSet);

            if (_map.TryGetValue(selectionSet, out var local))
            {
                values = local;
                return true;
            }
            else
            {
                values = null;
                return false;
            }
        }
        finally
        {
            if (taken)
            {
                Monitor.Exit(_map);
            }
        }
    }

    public void RegisterState(WorkItem value)
    {
        var taken = false;
        List<WorkItem>? values;
        Monitor.Enter(_map, ref taken);

        try
        {
            if (_immutable.Contains(value.SelectionSet))
            {
                throw new InvalidOperationException(
                    $"The state for the selection set `{value.SelectionSet.Id}` is immutable.");
            }

            if (!_map.TryGetValue(value.SelectionSet, out values))
            {
                var temp = new List<WorkItem> { value };
                _map.Add(value.SelectionSet, temp);
            }
        }
        finally
        {
            if (taken)
            {
                Monitor.Exit(_map);
            }
        }

        if (values is not null)
        {
            taken = false;
            Monitor.Enter(values, ref taken);

            try
            {
                values.Add(value);
            }
            finally
            {
                if (taken)
                {
                    Monitor.Exit(values);
                }
            }
        }
    }
}

