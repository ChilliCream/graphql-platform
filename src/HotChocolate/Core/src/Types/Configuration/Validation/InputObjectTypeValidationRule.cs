using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;
using static HotChocolate.Utilities.ErrorHelper;

namespace HotChocolate.Configuration.Validation;

internal sealed class InputObjectTypeValidationRule : ISchemaValidationRule
{
    public void Validate(
        IDescriptorContext context,
        ISchemaDefinition schema,
        ICollection<ISchemaError> errors)
    {
        if (!context.Options.StrictValidation)
        {
            return;
        }

        List<string>? names = null;
        var inputTypes = new List<InputObjectType>();

        foreach (var type in schema.Types)
        {
            if (type is not InputObjectType inputType)
            {
                continue;
            }

            EnsureTypeHasFields(inputType, errors);
            EnsureFieldNamesAreValid(inputType, errors);
            EnsureOneOfFieldsAreValid(inputType, errors, ref names);
            EnsureFieldDeprecationIsValid(inputType, errors);
            EnsureDefaultValuesAreValid(inputType, errors);

            inputTypes.Add(inputType);
        }

        EnsureNoUnbreakableCycles(inputTypes, errors);
    }

    /// <summary>
    /// Reports each unbreakable cycle among the Input Objects once. A OneOf Input Object
    /// requires every Input Object field; a non-OneOf Input Object requires only its non-null
    /// Input Object fields.
    /// </summary>
    private static void EnsureNoUnbreakableCycles(
        List<InputObjectType> inputTypes,
        ICollection<ISchemaError> errors)
    {
        var states = inputTypes.ConvertAll(t => new InputObjectState(t));
        var finiteStates = CollectEdges(states);

        PropagateFiniteValues(finiteStates);
        ReportCycles(states, errors);
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
    private static InputObjectType? GetCycleTarget(bool isOneOf, IType fieldType)
    {
        if (fieldType.Kind == TypeKind.NonNull)
        {
            fieldType = ((NonNullType)fieldType).NullableType;
        }
        else if (!isOneOf)
        {
            return null;
        }

        return fieldType as InputObjectType;
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

    private static void ReportCycles(
        List<InputObjectState> states,
        ICollection<ISchemaError> errors)
    {
        var context = new CycleReportContext(errors);

        foreach (var state in states)
        {
            if (!state.HasFiniteValue)
            {
                ReportCycles(state, context);
            }
        }
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
                context.Errors.Add(
                    InputObjectMustNotHaveUnbreakableCycle(edge.Target.Type, cyclePath));
            }
            else
            {
                ReportCycles(edge.Target, context);
            }

            context.FieldPath.Pop();
        }

        context.FieldPathIndex.Remove(state);
    }

    private static void EnsureOneOfFieldsAreValid(
        InputObjectType type,
        ICollection<ISchemaError> errors,
        ref List<string>? temp)
    {
        if (!type.Directives.ContainsDirective(DirectiveNames.OneOf.Name))
        {
            return;
        }

        temp ??= [];

        foreach (var field in type.Fields)
        {
            if (field.Type.Kind is TypeKind.NonNull || field.DefaultValue is not null)
            {
                temp.Add(field.Name);
            }
        }

        if (temp.Count == 0)
        {
            return;
        }

        var fieldNames = new string[temp.Count];
        for (var i = 0; i < temp.Count; i++)
        {
            fieldNames[i] = temp[i];
        }

        temp.Clear();
        errors.Add(OneOfInputObjectMustHaveNullableFieldsWithoutDefaults(type, fieldNames));
    }

    private sealed class InputObjectState(InputObjectType type)
    {
        public List<InputObjectState> Dependents { get; } = [];

        public List<CycleEdge> Edges { get; } = [];

        public bool HasFiniteValue { get; set; }

        public InputObjectType Type { get; } = type;

        public int UnresolvedEdgeCount { get; set; }
    }

    private readonly record struct CycleEdge(InputField Field, InputObjectState Target);

    private sealed class CycleReportContext(ICollection<ISchemaError> errors)
    {
        public ICollection<ISchemaError> Errors { get; } = errors;

        public List<string> FieldPath { get; } = [];

        public Dictionary<InputObjectState, int> FieldPathIndex { get; } = [];

        public HashSet<InputObjectState> Visited { get; } = [];
    }
}
