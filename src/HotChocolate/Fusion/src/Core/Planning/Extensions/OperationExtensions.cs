using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Planning;

internal static class OperationExtensions
{
    public static ISelectionSet GetSelectionSet(this IOperation operation, ExecutionStep step)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (step == null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        if (step.ParentSelection == null)
        {
            if (step.SelectionSetType == operation.RootType)
            {
                return operation.RootSelectionSet;
            }

            throw new ArgumentException($"{nameof(step)}.ParentSelection is null.", nameof(step));
        }

        return operation.GetSelectionSet(step.ParentSelection, step.SelectionSetType);
    }
}
