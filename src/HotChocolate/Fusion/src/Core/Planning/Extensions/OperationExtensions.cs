using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Planning;

public static class OperationExtensions
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
            throw new ArgumentException("step.ParentSelection is null.", nameof(step));
        }

        return operation.GetSelectionSet(step.ParentSelection, step.SelectionSetType);
    }
}
