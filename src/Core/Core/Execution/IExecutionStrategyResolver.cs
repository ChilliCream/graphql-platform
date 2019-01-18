using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal interface IExecutionStrategyResolver
    {
        IExecutionStrategy Resolve(OperationType operationType);
    }
}
