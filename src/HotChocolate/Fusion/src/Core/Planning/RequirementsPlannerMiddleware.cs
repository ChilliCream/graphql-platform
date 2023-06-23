using HotChocolate.Fusion.Metadata;
using HotChocolate.Utilities;
using static System.StringComparer;

namespace HotChocolate.Fusion.Planning;

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

        var schemas = new Dictionary<string, SelectionExecutionStep>(Ordinal);
        var requires = new HashSet<string>(Ordinal);

        foreach (var step in context.Steps)
        {
            if (step is SelectionExecutionStep currentStep &&
                currentStep.ParentSelection is { } parent &&
                currentStep.Resolver is not null)
            {
                var declaringType = currentStep.SelectionSetType;
                var selectionSet = context.Operation.GetSelectionSet(parent, declaringType);
                var siblingExecutionSteps = context.GetSiblingExecutionSteps(selectionSet);

                // remove the execution step for which we try to resolve dependencies.
                siblingExecutionSteps.Remove(currentStep);

                // clean and fill the schema execution step lookup
                foreach (var siblingExecutionStep in siblingExecutionSteps)
                {
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
                        out var providingExecutionStep))
                    {
                        currentStep.DependsOn.Add(providingExecutionStep);
                        currentStep.Variables.TryAdd(requirement, stateKey);
                        requires.Remove(requirement);
                    }
                }

                // if we still have requirements unfulfilled, we will try to resolve them
                // from sibling execution steps.
                foreach (var variable in step.SelectionSetTypeInfo.Variables)
                {
                    var subgraphName = variable.SubgraphName;

                    if (requires.Contains(variable.Name) &&
                        schemas.TryGetValue(subgraphName, out var providingExecutionStep))
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

                // it could happen that the existing execution steps cannot fulfill our needs
                // and that we have to introduce a fetch to another remote schema to get the
                // required value for the current execution step. In this case we will have
                // to evaluate the schemas that we did skip for efficiency reasons.
                // TODO: CODE

                if (requires.Count > 0)
                {
                    // if the schema meta data are not consistent we could end up with no way to
                    // execute the current execution step. In this case we will fail here.
                    // TODO : NEEDS A PROPER EXCEPTION
                    throw new Exception("NEEDS A PROPER EXCEPTION");
                }

                foreach (var (name, type) in currentStep.Resolver.ArgumentTypes)
                {
                    currentStep.ArgumentTypes.TryAdd(name, type);
                }

                // if we do by key batching the current execution step must
                // re-export its requirements so we know where entities belong to.
                if (currentStep.Resolver.Kind is ResolverKind.BatchByKey)
                {
                    foreach (var variable in step.SelectionSetTypeInfo.Variables)
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
}
