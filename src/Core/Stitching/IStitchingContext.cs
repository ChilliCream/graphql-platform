using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public interface IStitchingContext
    {
        IQueryExecuter GetQueryExecuter(string schemaName);
    }
}
