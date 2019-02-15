using HotChocolate.Execution;

namespace HotChocolate.Stitching.Delegation
{
    public interface IRemoteExecutorAccessor
    {
        string SchemaName { get; }
        IQueryExecutor Executor { get; }
    }
}
