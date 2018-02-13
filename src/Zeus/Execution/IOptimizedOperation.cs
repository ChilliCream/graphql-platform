using System;
using System.Collections.Generic;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    public interface IOptimizedOperation
    {
        ISchema Schema { get; }
        QueryDocument QueryDocument { get; }
        OperationDefinition Operation { get; }
        IReadOnlyCollection<IOptimizedSelection> Selections { get; }

        IResolverContext CreateContext(IServiceProvider services, 
            IVariableCollection variables, Action<IBatchedQuery> registerQuery);
    }
}