using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing
{
    internal static class ExecutionContextExtensions
    {
        public static IExecutionTask CreateTask(
            this IOperationContext operationContext,
            ResolverTaskDefinition taskDefinition)
        {
            ResolverTaskBase resolverTask =
                taskDefinition.Selection.PureResolver is null
                    ? operationContext.Execution.ResolverTasks.Get()
                    : operationContext.Execution.PureResolverTasks.Get();

            resolverTask.Initialize(
                taskDefinition.OperationContext,
                taskDefinition.Selection,
                taskDefinition.ResultMap,
                taskDefinition.ResponseIndex,
                taskDefinition.Parent,
                taskDefinition.Path,
                taskDefinition.ScopedContextData);

            return resolverTask;
        }

        public static BatchExecutionTask CreateBatchTask(
            this IOperationContext operationContext)
        {
            BatchExecutionTask batch = operationContext.Execution.BatchTasks.Get();
            batch.Initialize(operationContext);
            return batch;
        }
    }
}
