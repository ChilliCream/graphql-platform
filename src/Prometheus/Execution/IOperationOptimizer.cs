using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Prometheus.Abstractions;

namespace Prometheus.Execution
{
    public interface IOperationOptimizer
    {
        IOptimizedOperation Optimize(
            ISchema schema,
            IQueryDocument queryDocument,
            string operationName);
    }
}