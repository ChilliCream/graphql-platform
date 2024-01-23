using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Execution.Processing;
using static HotChocolate.Fusion.FusionResources;

namespace HotChocolate.Fusion.Execution;

internal sealed class RequestState
{
    private readonly Dictionary<SelectionSet, List<ExecutionState>> _map = new();
    private readonly HashSet<SelectionSet> _immutable = [];

    public bool ContainsState(SelectionSet selectionSet)
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

    public bool ContainsState(SelectionSet[] selectionSets)
    {
        var taken = false;
        Monitor.Enter(_map, ref taken);

        try
        {
            ref var start = ref MemoryMarshal.GetArrayDataReference(selectionSets);
            ref var end = ref Unsafe.Add(ref start, selectionSets.Length);

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                if (_map.ContainsKey(start))
                {
                    return true;
                }

                start = ref Unsafe.Add(ref start, 1)!;
            }
        }
        finally
        {
            if (taken)
            {
                Monitor.Exit(_map);
            }
        }

        return false;
    }

    public bool TryGetState(
        SelectionSet selectionSet,
        [NotNullWhen(true)] out List<ExecutionState>? values)
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

    public void TryRegisterState(
        SelectionSet selectionSet,
        ObjectResult result,
        IReadOnlyList<string> exportKeys,
        SelectionData parentData)
    {
        var taken = false;
        List<ExecutionState>? states;
        ExecutionState? state;
        Monitor.Enter(_map, ref taken);

        try
        {
            if (_immutable.Contains(selectionSet))
            {
                return;
            }

            state = new ExecutionState(selectionSet, result, exportKeys)
            {
                SelectionSetData = { [0] = parentData, },
            };

            if (!_map.TryGetValue(state.SelectionSet, out states))
            {
                var temp = new List<ExecutionState> { state, };
                _map.Add(state.SelectionSet, temp);
            }
        }
        finally
        {
            if (taken)
            {
                Monitor.Exit(_map);
            }
        }

        AddState(states, state);
    }

    public void RegisterState(ExecutionState state)
    {
        var taken = false;
        List<ExecutionState>? states;
        Monitor.Enter(_map, ref taken);

        try
        {
            if (_immutable.Contains(state.SelectionSet))
            {
                throw new InvalidOperationException(
                    string.Format(ExecutionState_RegisterState_StateImmutable, state.SelectionSet.Id));
            }

            if (!_map.TryGetValue(state.SelectionSet, out states))
            {
                var temp = new List<ExecutionState> { state, };
                _map.Add(state.SelectionSet, temp);
            }
        }
        finally
        {
            if (taken)
            {
                Monitor.Exit(_map);
            }
        }

        AddState(states, state);
    }

    private static void AddState(List<ExecutionState>? states, ExecutionState state)
    {
        if (states is null)
        {
            return;
        }

        var taken = false;
        Monitor.Enter(states, ref taken);

        try
        {
            states.Add(state);
        }
        finally
        {
            if (taken)
            {
                Monitor.Exit(states);
            }
        }
    }
}
