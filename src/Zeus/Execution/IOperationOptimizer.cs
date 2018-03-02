using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;

namespace Zeus.Execution
{
    public interface IOperationOptimizer
    {
        IOptimizedOperation Optimize(ISchema schema, QueryDocument queryDocument, string operationName);
    }
}