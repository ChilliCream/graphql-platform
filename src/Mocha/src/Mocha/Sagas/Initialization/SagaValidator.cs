namespace Mocha.Sagas;

internal static class SagaValidator
{
    public static void ValidateStateMachine(Saga saga)
    {
        var states = saga.States;

        if (states.Count == 0)
        {
            throw new SagaInitializationException(saga, "Saga has no states defined.");
        }

        if (!states.Any(x => x.Value.IsInitial))
        {
            throw new SagaInitializationException(saga, "No initial states found in the saga.");
        }

        var finalStates = new List<string>();
        var allStateNames = new HashSet<string>(states.Keys);

        // this is used to look up the states that can reach a given state
        var reverseAdjacency = new Dictionary<string, List<string>>(capacity: states.Count);

        foreach (var stateName in states.Keys)
        {
            reverseAdjacency[stateName] = [];
        }

        foreach (var (stateName, sagaState) in states)
        {
            if (sagaState.IsFinal)
            {
                finalStates.Add(stateName);
            }

            foreach (var transition in sagaState.Transitions.Values)
            {
                if (!allStateNames.Contains(transition.TransitionTo))
                {
                    throw new SagaInitializationException(
                        saga,
                        $"State '{stateName}' transitions to '{transition.TransitionTo}', which is not defined.");
                }

                // For a transition stateName -> transition.TransitionTo,
                // add: reverseAdjacency[transition.TransitionTo].Add(stateName)
                reverseAdjacency[transition.TransitionTo].Add(stateName);
            }
        }

        if (finalStates.Count == 0)
        {
            throw new SagaInitializationException(saga, "No final states found in the saga.");
        }

        var visited = new HashSet<string>();
        var queue = new Queue<string>();

        // Enqueue all final states initially
        foreach (var fs in finalStates)
        {
            visited.Add(fs);
            queue.Enqueue(fs);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var predecessor in reverseAdjacency[current])
            {
                if (visited.Add(predecessor))
                {
                    queue.Enqueue(predecessor);
                }
            }
        }

        var unreachableStates = states.Keys.Where(s => !visited.Contains(s)).ToArray();
        if (unreachableStates.Length > 0)
        {
            var unreachableList = string.Join(", ", unreachableStates);

            throw new SagaInitializationException(
                saga,
                "The following states cannot reach a final state: " + unreachableList);
        }
    }
}
