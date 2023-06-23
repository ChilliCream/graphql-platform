using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Utilities;
using static System.StringComparer;
using static HotChocolate.Fusion.Planning.PlanningUitilities;

namespace HotChocolate.Fusion.Planning;

internal sealed class FieldRequirementsPlannerMiddleware : IQueryPlanMiddleware
{
    private readonly FusionGraphConfiguration _config;

    public FieldRequirementsPlannerMiddleware(FusionGraphConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void Invoke(QueryPlanContext context, QueryPlanDelegate next)
    {
        Plan(context);
        next(context);
    }

    private void Plan(QueryPlanContext context)
    {
        context.ReBuildSelectionLookup();

        var fieldContext = new FieldContext();

        foreach (var step in context.Steps)
        {
            if (step is SelectionExecutionStep currentStep &&
                currentStep.ParentSelection is not null &&
                currentStep.SelectionResolvers.Count > 0)
            {
                ResolveRequirementsForSelectionResolvers(
                    context,
                    fieldContext,
                    currentStep,
                    currentStep.ParentSelection);
            }
        }

        context.Steps.AddRange(fieldContext.RequirementSteps);
    }

    private void ResolveRequirementsForSelectionResolvers(
        QueryPlanContext context,
        FieldContext fieldContext,
        SelectionExecutionStep currentStep,
        ISelection parentSelection)
    {
        foreach (var (selection, resolver) in currentStep.SelectionResolvers)
        {
            fieldContext.Schemas.Clear();

            var field = selection.Field;
            var declaringType = selection.DeclaringType;
            var selectionSet = context.Operation.GetSelectionSet(parentSelection, declaringType);
            var siblingExecutionSteps = GetSiblingExecutionSteps(context, selectionSet);

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
                    fieldContext.AllSubgraphs.IntersectWith(fieldContext.VariableSubgraphs);
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

        var selectionSet = parentSelection is null
            ? context.Operation.RootSelectionSet
            : context.Operation.GetSelectionSet(parentSelection, selection.DeclaringType);

        var resolver = SelectResolver(fieldContext, typeInfo, subgraph);

        var requirementStep = new SelectionExecutionStep(
            subgraph,
            parentSelection,
            selection.DeclaringType,
            typeInfo);

        foreach (var requirement in resolver.Requires)
        {
            requirementStep.Requires.Add(requirement);
        }

        requirementStep.Resolver = resolver;

        fieldContext.RequirementSteps.Add(requirementStep);

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

    private static ResolverDefinition SelectResolver(
        FieldContext fieldContext,
        ObjectTypeInfo typeInfo,
        string subgraph)
    {
        fieldContext.VariablesInContext.Clear();

        foreach (var variable in typeInfo.Variables)
        {
            fieldContext.VariablesInContext.Add(variable.Name);
        }

        if (!typeInfo.Resolvers.TryGetValue(subgraph, out var resolvers))
        {
            throw ThrowHelper.NoResolverInContext();
        }

        ResolverDefinition? selectedResolver = null;
        var requirements = 0;

        foreach (var resolver in resolvers)
        {
            if (!FulfillsRequirements(resolver, fieldContext.VariablesInContext))
            {
                continue;
            }

            if (requirements > resolver.Requires.Count || selectedResolver is null ||
                (selectedResolver.Requires.Count == resolver.Requires.Count &&
                    selectedResolver.Kind == ResolverKind.Query &&
                    (resolver.Kind == ResolverKind.Batch ||
                        resolver.Kind == ResolverKind.BatchByKey)))
            {
                requirements = resolver.Requires.Count;
                selectedResolver = resolver;
            }
        }

        if (selectedResolver is null)
        {
            throw ThrowHelper.NoResolverInContext();
        }

        return selectedResolver;

        static bool FulfillsRequirements(
            ResolverDefinition resolver,
            HashSet<string> variabesInContext)
        {
            foreach (var requirement in resolver.Requires)
            {
                if (!variabesInContext.Contains(requirement))
                {
                    return false;
                }
            }

            return true;
        }
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

            if (variable is FieldVariableDefinition fieldVariable &&
                fieldContext.Schemas.TryGetValue(variable.SubgraphName, out var providingExecutionStep))
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
        public readonly HashSet<string> VariablesInContext = new(Ordinal);
    }
}
