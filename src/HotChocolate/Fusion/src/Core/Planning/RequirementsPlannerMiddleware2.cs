using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Utilities;
using static System.StringComparer;

namespace HotChocolate.Fusion.Planning;

internal sealed class FieldRequirementsPlannerMiddleware : IQueryPlanMiddleware
{
    private readonly FusionGraphConfiguration _config;

    public FieldRequirementsPlannerMiddleware(FusionGraphConfiguration config)
    {
        _config = config;
    }

    public void Invoke(QueryPlanContext context, QueryPlanDelegate next)
    {
        Plan(context);
        next(context);
    }

    private void Plan(QueryPlanContext context)
    {
        var selectionLookup = CreateSelectionLookup(context.Steps);
        var schemas = new Dictionary<string, SelectionExecutionStep>(Ordinal);
        var requires = new HashSet<string>(Ordinal);

        foreach (var step in context.Steps)
        {
            if (step is SelectionExecutionStep currentStep &&
                currentStep.ParentSelection is { } parent &&
                currentStep.Resolver is not null)
            {
                var declaringType = currentStep.RootSelections[0].Selection.DeclaringType;
                var selectionSet = context.Operation.GetSelectionSet(parent, declaringType);
                var siblingExecutionSteps = GetSiblingExecutionSteps(selectionLookup, selectionSet);

                // remove the execution step for which we try to resolve dependencies.
                siblingExecutionSteps.Remove(currentStep);

                // clean and fill the schema execution step lookup
                foreach (var siblingExecutionStep in siblingExecutionSteps)
                {
                    schemas.TryAdd(siblingExecutionStep.SubgraphName, siblingExecutionStep);
                }

                foreach (var selection in currentStep.AllSelections)
                {
                    if (_config.TryGetType<ObjectTypeInfo>(selection.DeclaringType.Name, out var typeInfo) &&
                        typeInfo.Fields.TryGetField(selection.Field.Name, out var fieldInfo) &&
                        fieldInfo.Resolvers.TryGetValue(currentStep.SubgraphName, out var fieldResolvers) &&
                        fieldResolvers.Count > 0)
                    {
                        selectionSet = selection.DeclaringSelectionSet;
                        var selected = fieldResolvers[0];
                        var selectedCost = 10000;

                        if (fieldResolvers.Count > 1)
                        {
                            foreach (var resolver in fieldResolvers.OrderBy(t => t.Requires.Count))
                            {
                                var cost = 0;

                                foreach (var requirement in resolver.Requires)
                                {
                                    var inContext = false;

                                    foreach (var variable in fieldInfo.Variables.GetByName(
                                        requirement))
                                    {
                                        if (variable is ArgumentVariableDefinition)
                                        {
                                            inContext = true;
                                            break;
                                        }

                                        if (variable is FieldVariableDefinition &&
                                            schemas.ContainsKey(variable.SubgraphName))
                                        {
                                            inContext = true;
                                            break;
                                        }
                                    }

                                    if (!inContext)
                                    {
                                        cost++;
                                    }
                                }

                                if (selectedCost > cost)
                                {
                                    selected = resolver;
                                    selectedCost = cost;
                                }
                            }
                        }

                        foreach (var requirement in selected.Requires)
                        {
                            requires.Add(requirement);

                            if (context.Exports.TryGetStateKey(
                                selectionSet,
                                requirement,
                                out var stateKey,
                                out var providingExecutionStep))
                            {
                                currentStep.DependsOn.Add(providingExecutionStep);
                                currentStep.Variables.Add(requirement, stateKey);
                            }
                        }

                        foreach (var variable in fieldInfo.Variables)
                        {
                            if (!requires.Contains(variable.Name))
                            {
                                continue;
                            }

                            if (variable is ArgumentVariableDefinition)
                            {
                                requires.Remove(variable.Name);
                                continue;
                            }

                            if (variable is FieldVariableDefinition fieldVariable)
                            {
                                if (schemas.TryGetValue(
                                    variable.SubgraphName,
                                    out var providingExecutionStep))
                                {
                                    requires.Remove(variable.Name);

                                    var stateKey = context.Exports.Register(
                                        selectionSet,
                                        fieldVariable,
                                        providingExecutionStep);

                                    currentStep.DependsOn.Add(providingExecutionStep);
                                    currentStep.Variables.TryAdd(variable.Name, stateKey);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static HashSet<SelectionExecutionStep> GetSiblingExecutionSteps(
        Dictionary<object, SelectionExecutionStep> selectionLookup,
        ISelectionSet selectionSet)
    {
        var executionSteps = new HashSet<SelectionExecutionStep>();

        if (selectionLookup.TryGetValue(selectionSet, out var executionStep))
        {
            executionSteps.Add(executionStep);
        }

        foreach (var sibling in selectionSet.Selections)
        {
            if (selectionLookup.TryGetValue(sibling, out executionStep))
            {
                executionSteps.Add(executionStep);
            }
        }

        return executionSteps;
    }

    private static Dictionary<object, SelectionExecutionStep> CreateSelectionLookup(
        IReadOnlyList<ExecutionStep> executionSteps)
    {
        var dictionary = new Dictionary<object, SelectionExecutionStep>();

        foreach (var executionStep in executionSteps)
        {
            if (executionStep is SelectionExecutionStep ses)
            {
                foreach (var selection in ses.AllSelections)
                {
                    dictionary.TryAdd(selection, ses);
                }

                foreach (var selectionSet in ses.AllSelectionSets)
                {
                    dictionary.TryAdd(selectionSet, ses);
                }
            }
        }

        return dictionary;
    }

    private void GatherVariablesInContext(
        ISelection selection,
        ObjectTypeInfo declaringTypeInfo,
        HashSet<string> variablesInContext)
    {
        variablesInContext.Clear();

        foreach (var variable in declaringTypeInfo.Variables)
        {
            variablesInContext.Add(variable.Name);
        }

        var field = declaringTypeInfo.Fields[selection.Field.Name];

        foreach (var variable in field.Variables)
        {
            variablesInContext.Add(variable.Name);
        }
    }
}
