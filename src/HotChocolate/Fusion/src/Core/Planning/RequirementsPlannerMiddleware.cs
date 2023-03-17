using HotChocolate.Execution.Processing;
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
        var selectionLookup = CreateSelectionLookup(context.Steps);
        var schemas = new Dictionary<string, DefaultExecutionStep>(Ordinal);
        var requires = new HashSet<string>(Ordinal);

        foreach (var step in context.Steps)
        {
            if(step is NodeExecutionStep nodeExecutionStep)
            {
                var declaringType = nodeExecutionStep.NodeSelection.DeclaringType;
                var selectionSet = context.Operation.RootSelectionSet;



            }

            if (step is DefaultExecutionStep defaultExecutionStep &&
                defaultExecutionStep.ParentSelection is { } parent &&
                defaultExecutionStep.Resolver is not null)
            {
                var declaringType = defaultExecutionStep.RootSelections[0].Selection.DeclaringType;
                var selectionSet = context.Operation.GetSelectionSet(parent, declaringType);
                var siblingExecutionSteps = GetSiblingExecutionSteps(selectionLookup, selectionSet);

                // remove the execution step for which we try to resolve dependencies.
                siblingExecutionSteps.Remove(defaultExecutionStep);

                // clean and fill the schema execution step lookup
                foreach (var siblingExecutionStep in siblingExecutionSteps)
                {
                    schemas.TryAdd(siblingExecutionStep.SubgraphName, siblingExecutionStep);
                }

                // clean and fill requires set
                InitializeSet(requires, defaultExecutionStep.Requires);

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
                        defaultExecutionStep.DependsOn.Add(providingExecutionStep);
                        defaultExecutionStep.Variables.Add(requirement, stateKey);
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

                        defaultExecutionStep.DependsOn.Add(providingExecutionStep);
                        defaultExecutionStep.Variables.Add(variable.Name, stateKey);
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

                // if we do by key batching the current execution step must
                // re-export its requirements.
                if (defaultExecutionStep.Resolver.Kind is ResolverKind.BatchByKey)
                {
                    foreach (var variable in step.SelectionSetTypeInfo.Variables)
                    {
                        if (defaultExecutionStep.Requires.Contains(variable.Name) &&
                            defaultExecutionStep.SubgraphName.EqualsOrdinal(variable.SubgraphName) &&
                            context.Exports.TryGetStateKey(
                                selectionSet,
                                variable.Name,
                                out var stateKey,
                                out _))
                        {
                            context.Exports.RegisterAdditionExport(
                                variable,
                                defaultExecutionStep,
                                stateKey);
                        }
                    }
                }
            }
        }
    }

    private static HashSet<DefaultExecutionStep> GetSiblingExecutionSteps(
        Dictionary<object, DefaultExecutionStep> selectionLookup,
        ISelectionSet selectionSet)
    {
        var executionSteps = new HashSet<DefaultExecutionStep>();

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

    private static Dictionary<object, DefaultExecutionStep> CreateSelectionLookup(
        IReadOnlyList<ExecutionStep> executionSteps)
    {
        var dictionary = new Dictionary<object, DefaultExecutionStep>();

        foreach (var executionStep in executionSteps)
        {
            if (executionStep is DefaultExecutionStep ses)
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

    private static void InitializeSet(HashSet<string> set, IEnumerable<string> values)
    {
        set.Clear();

        foreach (var value in values)
        {
            set.Add(value);
        }
    }
}
