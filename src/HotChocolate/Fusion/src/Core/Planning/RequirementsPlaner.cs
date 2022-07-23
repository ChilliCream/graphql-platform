using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Planning;

internal sealed class RequirementsPlaner
{
    private readonly Metadata.Schema _schema;

    public RequirementsPlaner(Metadata.Schema schema)
    {
        _schema = schema;
    }

    public void Plan(IOperation operation, IReadOnlyList<QueryPlanWorkItem> executionSteps)
    {
        var selectionLookup = CreateSelectionLookup(executionSteps);
        var schemas = new HashSet<string>(StringComparer.Ordinal);
        var requires = new HashSet<string>(StringComparer.Ordinal);
        var variableGroup = Guid.NewGuid().ToString("N")[24..];
        var variableId = 0;

        foreach (var executionStep in executionSteps.Where(static t => t.Requires.Count > 0))
        {
            if (executionStep.ParentSelection is { } parent &&
                executionStep.Resolver is { } resolver)
            {
                var declaringType = executionStep.RootSelections[0].Selection.DeclaringType;
                var siblingSelectionSet = operation.GetSelectionSet(parent, declaringType);
                var siblings = siblingSelectionSet.Selections;
                var siblingExecutionSteps = GetSiblingExecutionSteps(selectionLookup, siblings);

                // remove the execution step for which we try to resolve dependencies.
                siblingExecutionSteps.Remove(executionStep);

                // clean and fill preferred schemas set
                InitializeSet(schemas, siblingExecutionSteps.Select(static t => t.SchemaName));

                // clean and fill requires set
                InitializeSet(requires, executionStep.Requires);

                foreach (var variable in executionStep.DeclaringType.Variables)
                {
                    var schemaName = variable.SchemaName;
                    if (schemas.Contains(schemaName) && requires.Contains(variable.Name))
                    {
                        requires.Remove(variable.Name);

                        // first we need to check if the selectionSet from which we want to do the
                        // export already is exporting the required variable
                        // if so we need to just refer to it.
                        var variableName = $"_{variableGroup}_{++variableId}";
                        var siblingExecutionStep = siblingExecutionSteps.First(t => string.Equals(t.SchemaName, schemaName, StringComparison.Ordinal));
                        // we need to annotate the export with the selectionSet
                        // from which we are exporting.
                    }
                }

                // it could happen that the existing execution steps cannot fulfill our needs
                // and that we have to introduce a fetch to another remote schema to get the
                // required value for the current execution step. In this case we will have
                // to evaluate the schemas that we did skip for efficiency reasons.
                // CODE

                // if the schema meta data are not consistent we could end up with no way to
                // execute the current execution step. In this case we will fail here.
                throw new Exception("NEEDS A PROPER EXCEPTION");
            }
        }
    }

    private static HashSet<QueryPlanWorkItem> GetSiblingExecutionSteps(
        Dictionary<ISelection, QueryPlanWorkItem>  selectionLookup,
        IReadOnlyList<ISelection> siblings)
    {
        var executionSteps = new HashSet<QueryPlanWorkItem>();

        foreach (var sibling in siblings)
        {
            if (selectionLookup.TryGetValue(sibling, out var executionStep))
            {
                executionSteps.Add(executionStep);
            }
        }

        return executionSteps;
    }

    private static Dictionary<ISelection, QueryPlanWorkItem> CreateSelectionLookup(
        IReadOnlyList<QueryPlanWorkItem> executionSteps)
    {
        var dictionary = new Dictionary<ISelection, QueryPlanWorkItem>();

        foreach (var executionStep in executionSteps)
        {
            foreach (var selection in executionStep.AllSelections)
            {
                dictionary.Add(selection, executionStep);
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
