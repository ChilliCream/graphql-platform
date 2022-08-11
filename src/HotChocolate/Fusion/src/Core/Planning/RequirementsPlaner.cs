using HotChocolate.Execution.Processing;
using static System.StringComparer;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The requirements planer will analyze the requirements for each
/// request to a downstream service and enrich these so that all requirements for each requests
/// are fulfilled.
/// </summary>
internal sealed class RequirementsPlaner
{
    public void Plan(QueryPlanContext context)
    {
        var selectionLookup = CreateSelectionLookup(context.Steps);
        var schemas = new Dictionary<string, SelectionExecutionStep>(Ordinal);
        var requires = new HashSet<string>(Ordinal);

        foreach (var step in context.Steps)
        {
            if (step is SelectionExecutionStep executionStep &&
                executionStep.ParentSelection is { } parent &&
                executionStep.Resolver is { })
            {
                var declaringType = executionStep.RootSelections[0].Selection.DeclaringType;
                var selectionSet = context.Operation.GetSelectionSet(parent, declaringType);
                var siblingExecutionSteps = GetSiblingExecutionSteps(selectionLookup, selectionSet);

                // remove the execution step for which we try to resolve dependencies.
                siblingExecutionSteps.Remove(executionStep);

                // clean and fill the schema execution step lookup
                foreach (var siblingExecutionStep in siblingExecutionSteps)
                {
                    schemas.TryAdd(siblingExecutionStep.SchemaName, siblingExecutionStep);
                }

                // clean and fill requires set
                InitializeSet(requires, executionStep.Requires);

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
                        executionStep.DependsOn.Add(providingExecutionStep);
                        executionStep.Variables.Add(requirement, stateKey);
                    }
                }

                // if we still have requirements unfulfilled will try to resolve them
                // from sibling execution steps.
                foreach (var variable in step.SelectionSetType.Variables)
                {
                    var schemaName = variable.SchemaName;
                    if (requires.Contains(variable.Name) &&
                        schemas.TryGetValue(schemaName, out var providingExecutionStep))
                    {
                        requires.Remove(variable.Name);

                        var stateKey = context.Exports.Register(
                            selectionSet,
                            variable,
                            providingExecutionStep);

                        executionStep.DependsOn.Add(providingExecutionStep);
                        executionStep.Variables.Add(variable.Name, stateKey);
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
            }
        }
    }

    private static HashSet<SelectionExecutionStep> GetSiblingExecutionSteps(
        Dictionary<object, SelectionExecutionStep>  selectionLookup,
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
        IReadOnlyList<IExecutionStep> executionSteps)
    {
        var dictionary = new Dictionary<object, SelectionExecutionStep>();

        foreach (var executionStep in executionSteps)
        {
            if (executionStep is SelectionExecutionStep ses)
            {
                foreach (var selection in ses.AllSelections)
                {
                    dictionary.Add(selection, ses);
                }

                foreach (var selectionSet in ses.AllSelectionSets)
                {
                    dictionary.Add(selectionSet, ses);
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
