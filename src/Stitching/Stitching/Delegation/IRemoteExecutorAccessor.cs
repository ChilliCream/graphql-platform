using HotChocolate.Execution;

namespace HotChocolate.Stitching.Delegation
{
    public interface IRemoteExecutorAccessor
    {
        NameString SchemaName { get; }

        IQueryExecutor Executor { get; }
    }
}
