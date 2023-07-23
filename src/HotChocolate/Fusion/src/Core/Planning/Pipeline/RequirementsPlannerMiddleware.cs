using HotChocolate.Fusion.Metadata;
using HotChocolate.Utilities;
using static System.StringComparer;

namespace HotChocolate.Fusion.Planning.Pipeline;

/// <summary>
/// The requirements planer will analyze the requirements for each
/// request to a downstream service and enrich these so that all requirements for each requests
/// are fulfilled.
/// </summary>
internal sealed class RequirementsPlannerMiddleware : IQueryPlanMiddleware
{
    public void Invoke(QueryPlanContext context, QueryPlanDelegate next)
    {
        Plan(context);
        next(context);
    }

    private static void Plan(QueryPlanContext context)
    {
        context.ReBuildSelectionLookup();

        var siblingsToRemove = new List<SelectionExecutionStep>();
        var schemas = new Dictionary<string, SelectionExecutionStep>(Ordinal);
        var requires = new HashSet<string>(Ordinal);
        var roots = new HashSet<string>(Ordinal);

        foreach (var step in context.Steps)
        {
            if (step is SelectionExecutionStep currentStep &&
                currentStep.ParentSelection is { } parent &&
                currentStep.Resolver is not null)
            {
                siblingsToRemove.Clear();

                var declaringType = currentStep.SelectionSetType;
                var selectionSet = context.Operation.GetSelectionSet(parent, declaringType);
                var siblingExecutionSteps = context.GetSiblingExecutionSteps(selectionSet);

                // remove the execution step for which we try to resolve dependencies.
                siblingExecutionSteps.Remove(currentStep);

                // remove all execution steps that depend on the current execution step.
                foreach (var subling in siblingExecutionSteps)
                {
                    if (subling is SelectionExecutionStep siblingExecutionStep &&
                        siblingExecutionStep.DependsOn.Contains(currentStep))
                    {
                        siblingsToRemove.Add(siblingExecutionStep);
                    }
                }

                foreach (var siblingToRemove in siblingsToRemove)
                {
                    siblingExecutionSteps.Remove(siblingToRemove);
                }

                // clean and fill the schema execution step lookup
                foreach (var siblingExecutionStep in siblingExecutionSteps)
                {
                    if (siblingExecutionStep.ParentSelection is null)
                    {
                        roots.Add(siblingExecutionStep.SubgraphName);
                    }

                    schemas.TryAdd(siblingExecutionStep.SubgraphName, siblingExecutionStep);
                }

                // clean and fill requires set
                InitializeSet(requires, currentStep.Requires);

                // first we need to check if the selectionSet from which we want to do the
                // exports already is exporting the required variables
                // if so we just need to refer to it.
                foreach (var requirement in requires)
                {
                    if (context.Exports.TryGetStateKey(
                        selectionSet,
                        requirement,
                        out var stateKey,
                        out var providingExecutionStep) &&
                        providingExecutionStep != currentStep)
                    {
                        currentStep.DependsOn.Add(providingExecutionStep);
                        currentStep.Variables.TryAdd(requirement, stateKey);
                        requires.Remove(requirement);
                    }
                }

                // if we still have requirements unfulfilled, we will try to resolve them
                // from sibling execution steps.
                // we prime the variables list with the variables from execution steps that the current step
                // already depends on.
                var variables = OrderByUsage(step.SelectionSetTypeMetadata.Variables, currentStep);

                // if we have root steps as siblings we will prfer to fulfill the requirements
                // from these steps.
                if (roots.Count > 0 && requires.Count > 0)
                {
                    foreach (var variable in variables)
                    {
                        if (requires.Contains(variable.Name) &&
                            roots.Contains(variable.SubgraphName) &&
                            schemas.TryGetValue(variable.SubgraphName, out var providingExecutionStep))
                        {
                            requires.Remove(variable.Name);

                            var stateKey = context.Exports.Register(
                                selectionSet,
                                variable,
                                providingExecutionStep);

                            currentStep.DependsOn.Add(providingExecutionStep);
                            currentStep.Variables.TryAdd(variable.Name, stateKey);
                        }
                    }
                }

                if (requires.Count > 0)
                {
                    foreach (var variable in variables)
                    {
                        if (requires.Contains(variable.Name) &&
                            schemas.TryGetValue(variable.SubgraphName, out var providingExecutionStep))
                        {
                            requires.Remove(variable.Name);

                            var stateKey = context.Exports.Register(
                                selectionSet,
                                variable,
                                providingExecutionStep);

                            currentStep.DependsOn.Add(providingExecutionStep);
                            currentStep.Variables.TryAdd(variable.Name, stateKey);
                        }
                    }
                }

                // it could happen that the existing execution steps cannot fulfill our needs
                // and that we have to introduce a fetch to another remote schema to get the
                // required value for the current execution step. In this case we will have
                // to evaluate the schemas that we did skip for efficiency reasons.
                if (requires.Count > 0)
                {
                    // if the schema meta data are not consistent we could end up with no way to
                    // execute the current execution step. In this case we will fail here.
                    throw new InvalidOperationException("The schema metadata are not consistent.");
                }

                foreach (var (name, type) in currentStep.Resolver.ArgumentTypes)
                {
                    currentStep.ArgumentTypes.TryAdd(name, type);
                }

                // if we do by key batching the current execution step must
                // re-export its requirements so we know where entities belong to.
                if (currentStep.Resolver.Kind is ResolverKind.BatchByKey)
                {
                    foreach (var variable in step.SelectionSetTypeMetadata.Variables)
                    {
                        if (currentStep.Requires.Contains(variable.Name) &&
                            currentStep.SubgraphName.EqualsOrdinal(variable.SubgraphName) &&
                            context.Exports.TryGetStateKey(
                                selectionSet,
                                variable.Name,
                                out var stateKey,
                                out _))
                        {
                            context.Exports.RegisterAdditionExport(
                                variable,
                                currentStep,
                                stateKey);
                        }
                    }
                }
            }
        }
    }

    private static void InitializeSet(HashSet<string> set, IEnumerable<string> values)
    {
        set.Clear();

        foreach (var value in values)
        {
            set.Add(value);
        }
    }

    private static List<FieldVariableDefinition> OrderByUsage(
        VariableDefinitionCollection variables,
        SelectionExecutionStep executionStep)
    {
        var dependsOnSubgraph = new HashSet<string>(Ordinal);

        foreach (var step in executionStep.DependsOn)
        {
            if (step is SelectionExecutionStep { SubgraphName: { } subgraphName })
            {
                dependsOnSubgraph.Add(subgraphName);
            }
        }

        var comparer = new VariableUsageComparer(dependsOnSubgraph);
        var ordered = new List<FieldVariableDefinition>(variables);
        ordered.Sort(comparer);
        return ordered;
    }
}
