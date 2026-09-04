using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Logging;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// An Input Object must not reference itself through a cycle that cannot be broken. A field
/// breaks a cycle when its type is a list, a nullable type on a non-OneOf Input Object, a type
/// other than an Input Object, or an Input Object that can itself be provided a finite value.
/// </summary>
/// <seealso href="https://github.com/graphql/graphql-spec/pull/1211">
/// Specification
/// </seealso>
public sealed class NoInputObjectUnbreakableCycleRule
    : IValidationEventHandler<InputObjectTypesEvent>
{
    /// <summary>
    /// Checks that every Input Object can be provided a finite value.
    /// </summary>
    public void Handle(InputObjectTypesEvent @event, ValidationContext context)
    {
        var states = @event.InputObjectTypes.Select(t => new InputObjectState(t)).ToList();
        var finiteStates = CollectEdges(states);

        PropagateFiniteValues(finiteStates);

        context.Log.Write(ReportCycles(states));
    }

    /// <summary>
    /// Records the fields through which each Input Object requires another Input Object: every
    /// Input Object field of a OneOf Input Object, and every non-null Input Object field of a
    /// non-OneOf Input Object. Returns the states that are finite on their own: a OneOf Input
    /// Object with a field that requires nothing, or a non-OneOf Input Object with no requiring
    /// field.
    /// </summary>
    private static Stack<InputObjectState> CollectEdges(List<InputObjectState> states)
    {
        var statesByType = states.ToDictionary(s => s.Type);
        var finiteStates = new Stack<InputObjectState>();

        foreach (var state in states)
        {
            foreach (var field in state.Type.Fields)
            {
                // Only a field that requires another Input Object of this schema forms an edge.
                if (GetCycleTarget(state.Type.IsOneOf, field.Type) is not { } targetType
                    || !statesByType.TryGetValue(targetType, out var target))
                {
                    continue;
                }

                state.Edges.Add(new CycleEdge(field, target));
                target.Dependents.Add(state);
            }

            var fieldCount = state.Type.Fields.Count;

            // A OneOf Input Object is finite on its own when it has no fields or when any field
            // forms no edge. A non-OneOf Input Object is finite on its own when no field forms an
            // edge.
            if (state.Type.IsOneOf
                ? fieldCount == 0 || state.Edges.Count < fieldCount
                : state.Edges.Count == 0)
            {
                MarkFinite(state, finiteStates);
            }
            else
            {
                state.UnresolvedEdgeCount = state.Edges.Count;
            }
        }

        return finiteStates;
    }

    /// <summary>
    /// Returns the Input Object that a field of the given type requires a value for, or
    /// <c>null</c> when the field type is an escape: a list, a nullable type on a non-OneOf
    /// Input Object, or a type other than an Input Object.
    /// </summary>
    private static IInputObjectTypeDefinition? GetCycleTarget(bool isOneOf, IType fieldType)
    {
        if (fieldType.Kind == TypeKind.NonNull)
        {
            fieldType = ((NonNullType)fieldType).NullableType;
        }
        else if (!isOneOf)
        {
            return null;
        }

        return fieldType as IInputObjectTypeDefinition;
    }

    private static void MarkFinite(InputObjectState state, Stack<InputObjectState> finiteStates)
    {
        state.HasFiniteValue = true;
        finiteStates.Push(state);
    }

    /// <summary>
    /// Marks as finite every Input Object whose requirement is met by the finite states found
    /// so far: a OneOf Input Object once any of its required Input Objects is finite, and a
    /// non-OneOf Input Object once all of them are.
    /// </summary>
    private static void PropagateFiniteValues(Stack<InputObjectState> finiteStates)
    {
        while (finiteStates.TryPop(out var finiteState))
        {
            foreach (var dependent in finiteState.Dependents)
            {
                if (dependent.HasFiniteValue)
                {
                    continue;
                }

                if (dependent.Type.IsOneOf || --dependent.UnresolvedEdgeCount == 0)
                {
                    MarkFinite(dependent, finiteStates);
                }
            }
        }
    }

    private static List<LogEntry> ReportCycles(List<InputObjectState> states)
    {
        var context = new CycleReportContext();

        foreach (var state in states)
        {
            if (!state.HasFiniteValue)
            {
                ReportCycles(state, context);
            }
        }

        return context.LogEntries;
    }

    private static void ReportCycles(InputObjectState state, CycleReportContext context)
    {
        if (!context.Visited.Add(state))
        {
            return;
        }

        context.FieldPathIndex[state] = context.FieldPath.Count;

        foreach (var edge in state.Edges)
        {
            if (edge.Target.HasFiniteValue)
            {
                continue;
            }

            context.FieldPath.Push(edge.Field.Coordinate.ToString());

            if (context.FieldPathIndex.TryGetValue(edge.Target, out var cycleIndex))
            {
                var cyclePath = context.FieldPath.Skip(cycleIndex);
                context.LogEntries.Add(InputObjectUnbreakableCycle(edge.Target.Type, cyclePath));
            }
            else
            {
                ReportCycles(edge.Target, context);
            }

            context.FieldPath.Pop();
        }

        context.FieldPathIndex.Remove(state);
    }

    private sealed class InputObjectState(IInputObjectTypeDefinition type)
    {
        public List<InputObjectState> Dependents { get; } = [];

        public List<CycleEdge> Edges { get; } = [];

        public bool HasFiniteValue { get; set; }

        public IInputObjectTypeDefinition Type { get; } = type;

        public int UnresolvedEdgeCount { get; set; }
    }

    private readonly record struct CycleEdge(IInputValueDefinition Field, InputObjectState Target);

    private sealed class CycleReportContext
    {
        public List<string> FieldPath { get; } = [];

        public Dictionary<InputObjectState, int> FieldPathIndex { get; } = [];

        public List<LogEntry> LogEntries { get; } = [];

        public HashSet<InputObjectState> Visited { get; } = [];
    }
}
