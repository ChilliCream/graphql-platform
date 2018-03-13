using System;
using System.Collections.Generic;
using Prometheus.Abstractions;
using Prometheus.Resolvers;

namespace Prometheus.Execution
{
    public interface IOptimizedOperation
    {
        ISchema Schema { get; }
        IQueryDocument QueryDocument { get; }
        OperationDefinition Operation { get; }
        IReadOnlyCollection<IOptimizedSelection> Selections { get; }

        IResolverContext CreateContext(IServiceProvider services, 
            IVariableCollection variables, Action<IBatchedQuery> registerQuery);
    }
}