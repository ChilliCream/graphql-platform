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
        var fieldContext = new FieldContext();

        foreach (var step in context.Steps)
        {
            if (step is SelectionExecutionStep currentStep &&
                currentStep.ParentSelection is not null &&
                currentStep.SelectionResolvers.Count > 0)
            {
                foreach (var (selection, resolver) in currentStep.SelectionResolvers)
                {
                    fieldContext.Schemas.Clear();

                    var field = selection.Field;
                    var declaringType = selection.DeclaringType;
                    var selectionSet = context.Operation.GetSelectionSet(
                        currentStep.ParentSelection,
                        declaringType);
                    var siblingExecutionSteps =
                        GetSiblingExecutionSteps(selectionLookup, selectionSet);

                    // remove the execution step for which we try to resolve dependencies.
                    siblingExecutionSteps.Remove(currentStep);

                    // clean and fill the schema execution step lookup
                    foreach (var siblingExecutionStep in siblingExecutionSteps)
                    {
                        fieldContext.Schemas.TryAdd(
                            siblingExecutionStep.SubgraphName,
                            siblingExecutionStep);
                    }

                    if (_config.TryGetType<ObjectTypeInfo>(declaringType.Name, out var typeInfo) &&
                        typeInfo.Fields.TryGetField(field.Name, out var fieldInfo))
                    {
                        ResolveVariablesInContext(
                            context,
                            fieldContext,
                            currentStep,
                            fieldInfo,
                            selectionSet,
                            resolver);

                        if (fieldContext.Requires.Count > 0)
                        {
                            ResolveVariableRequirements(
                                context,
                                fieldContext,
                                currentStep,
                                selection,
                                typeInfo,
                                fieldInfo);
                        }
                    }
                }
            }
        }

        context.Steps.AddRange(fieldContext.RequirementSteps);
    }

    private static void ResolveVariableRequirements(
        QueryPlanContext context,
        FieldContext fieldContext,
        SelectionExecutionStep currentStep,
        ISelection selection,
        ObjectTypeInfo typeInfo,
        ObjectFieldInfo fieldInfo)
    {
        fieldContext.Variables.AddRange(
            fieldInfo.Variables.OfType<FieldVariableDefinition>()
                .Where(t => fieldContext.Requires.Contains(t.Name))
                .Select(t => new VariableInfo(t.Name, t.SubgraphName, t))
                .GroupBy(t => t.Name)
                .OrderByDescending(t => t.Count()));

        while (fieldContext.Variables.Count > 0)
        {
            fieldContext.Selected.Clear();

            var first = fieldContext.Variables[0];
            fieldContext.Selected.Add(first);

            if (fieldContext.Variables.Count > 1)
            {
                DeterminePossibleSubgraphs(first, typeInfo, fieldContext.AllSubgraphs);
                GroupVariables(fieldContext);
            }

            RegisterRequirementStep(
                context,
                fieldContext,
                currentStep,
                selection,
                typeInfo,
                fieldContext.AllSubgraphs.First());

            foreach (var item in fieldContext.Selected)
            {
                fieldContext.Variables.Remove(item);
            }
        }
    }

    private static void DeterminePossibleSubgraphs(
        IGrouping<string, VariableInfo> variable,
        ObjectTypeInfo typeInfo,
        HashSet<string> allSubgraphs)
    {
        allSubgraphs.Clear();

        foreach (var item in variable)
        {
            if (typeInfo.Resolvers.ContainsResolvers(item.SubgraphName))
            {
                allSubgraphs.Add(item.SubgraphName);
            }
        }
    }

    private static void GroupVariables(FieldContext fieldContext)
    {
        for (var i = fieldContext.Variables.Count - 1; i >= 1; i--)
        {
            var current = fieldContext.Variables[i];

            fieldContext.VariableSubgraphs.Clear();

            foreach (var item in current)
            {
                fieldContext.VariableSubgraphs.Add(item.SubgraphName);
            }

            fieldContext.VariableSubgraphs.IntersectWith(fieldContext.AllSubgraphs);

            if (fieldContext.VariableSubgraphs.Count > 0)
            {
                if (fieldContext.AllSubgraphs.Count > fieldContext.VariableSubgraphs.Count)
                {
                    fieldContext.AllSubgraphs.IntersectWith(fieldContext.AllSubgraphs);
                }

                fieldContext.VariableSubgraphs.Clear();
                fieldContext.Selected.Add(current);
            }
        }
    }

    private static void RegisterRequirementStep(
        QueryPlanContext context,
        FieldContext fieldContext,
        SelectionExecutionStep currentStep,
        ISelection selection,
        ObjectTypeInfo typeInfo,
        string subgraph)
    {
        context.ParentSelections.TryGetValue(selection, out var parentSelection);
        var requirementStep = new SelectionExecutionStep(subgraph, typeInfo, parentSelection);
        fieldContext.RequirementSteps.Add(requirementStep);

        var selectionSet = parentSelection is null
            ? context.Operation.RootSelectionSet
            : context.Operation.GetSelectionSet(parentSelection, selection.DeclaringType);

        foreach (var variable in fieldContext.Selected)
        {
            var stateKey = context.Exports.Register(
                selectionSet,
                variable.First(t => t.SubgraphName.EqualsOrdinal(subgraph)).Variable,
                requirementStep);

            currentStep.DependsOn.Add(requirementStep);
            currentStep.Variables.TryAdd(variable.Key, stateKey);
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

    private static void ResolveVariablesInContext(
        QueryPlanContext context,
        FieldContext fieldContext,
        SelectionExecutionStep executionStep,
        ObjectFieldInfo fieldInfo,
        ISelectionSet selectionSet,
        ResolverDefinition resolver)
    {
        foreach (var requirement in resolver.Requires)
        {
            fieldContext.Requires.Add(requirement);

            if (context.Exports.TryGetStateKey(
                selectionSet,
                requirement,
                out var stateKey,
                out var providingExecutionStep))
            {
                executionStep.DependsOn.Add(providingExecutionStep);
                executionStep.Variables.Add(requirement, stateKey);
            }
        }

        foreach (var (name, type) in resolver.ArgumentTypes)
        {
            executionStep.ArgumentTypes.TryAdd(name, type);
        }

        foreach (var variable in fieldInfo.Variables)
        {
            if (!fieldContext.Requires.Contains(variable.Name))
            {
                continue;
            }

            if (variable is ArgumentVariableDefinition)
            {
                fieldContext.Requires.Remove(variable.Name);
                continue;
            }

            if (variable is FieldVariableDefinition fieldVariable)
            {
                if (fieldContext.Schemas.TryGetValue(
                    variable.SubgraphName,
                    out var providingExecutionStep))
                {
                    fieldContext.Requires.Remove(variable.Name);

                    var stateKey = context.Exports.Register(
                        selectionSet,
                        fieldVariable,
                        providingExecutionStep);

                    executionStep.DependsOn.Add(providingExecutionStep);
                    executionStep.Variables.TryAdd(variable.Name, stateKey);
                }
            }
        }
    }

    private sealed record VariableInfo(
        string Name,
        string SubgraphName,
        FieldVariableDefinition Variable);

    private sealed class FieldContext
    {
        public readonly Dictionary<string, SelectionExecutionStep> Schemas = new(Ordinal);
        public readonly HashSet<string> Requires = new(Ordinal);
        public readonly List<ExecutionStep> RequirementSteps = new();
        public readonly HashSet<string> AllSubgraphs = new();
        public readonly HashSet<string> VariableSubgraphs = new();
        public readonly List<IGrouping<string, VariableInfo>> Variables = new();
        public readonly List<IGrouping<string, VariableInfo>> Selected = new();
    }
}
