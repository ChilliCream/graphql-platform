using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public interface IStitchingContext
    {
        IQueryExecutor GetQueryExecutor(string schemaName);
    }
}
