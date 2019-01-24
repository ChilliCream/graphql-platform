using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public interface IRemoteExecutorAccessor
    {
        string SchemaName { get; }
        IQueryExecutor Executor { get; }
    }
}
