namespace HotChocolate.Execution.Processing.Tasks;

internal interface IResolverTask : IExecutionTask
{
    SelectionPath FieldSelectionPath { get; }
}
