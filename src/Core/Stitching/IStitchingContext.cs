using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public interface IStitchingContext
    {
        IQueryExecuter GetQueryExecuter(string schemaName);
    }
}
