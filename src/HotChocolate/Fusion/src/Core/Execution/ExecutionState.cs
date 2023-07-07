using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using HotChocolate.Execution.Processing;
using static HotChocolate.Fusion.FusionResources;

namespace HotChocolate.Fusion.Execution;

internal sealed class ExecutionState
{
    private readonly Dictionary<ISelectionSet, List<SelectionSetState>> _map = new();
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

    public bool ContainsState(ISelectionSet[] selectionSets)
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
        ISelectionSet selectionSet,
        [NotNullWhen(true)] out IReadOnlyList<SelectionSetState>? values)
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
        ISelectionSet selectionSet,
        ObjectResult result,
        IReadOnlyList<string> exportKeys,
        SelectionData parentData)
    {
        var taken = false;
        List<SelectionSetState>? states;
        SelectionSetState? state = null;
        Monitor.Enter(_map, ref taken);

        try
        {
            if (_immutable.Contains(selectionSet))
            {
                return;
            }
            
            state = new SelectionSetState(selectionSet, result, exportKeys)
            {
                SelectionSetData = { [0] = parentData }
            };
            
            if (!_map.TryGetValue(state.SelectionSet, out states))
            {
                var temp = new List<SelectionSetState> { state };
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

        if (states is not null)
        {
            taken = false;
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

    public void RegisterState(SelectionSetState state)
    {
        var taken = false;
        List<SelectionSetState>? states;
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
                var temp = new List<SelectionSetState> { state };
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

        if (states is not null)
        {
            taken = false;
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
}
