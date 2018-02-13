using System;
using System.Collections.Generic;
using System.Linq;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    public class OptimizedOperation
        : IOptimizedOperation
    {
        private readonly OperationContext _operationContext;

        public OptimizedOperation(
            ISchema schema,
            QueryDocument queryDocument,
            OperationDefinition operation,
            IEnumerable<IOptimizedSelection> selections)
        {
            _operationContext = new OperationContext(
                schema, queryDocument, operation);
            Selections = selections == null
                ? Array.Empty<IOptimizedSelection>()
                : selections.ToArray();
        }

        public ISchema Schema => _operationContext.Schema;

        public QueryDocument QueryDocument => _operationContext.QueryDocument;

        public OperationDefinition Operation => _operationContext.Operation;

        public IReadOnlyCollection<IOptimizedSelection> Selections { get; }

        public IResolverContext CreateContext(
            IServiceProvider services, 
            IVariableCollection variables, 
            Action<IBatchedQuery> registerQuery)
        {
            if (registerQuery == null)
            {
                throw new ArgumentNullException(nameof(registerQuery));
            }
            
            return ResolverContext.Create(services, _operationContext, 
                k => variables.GetVariable<object>(k), registerQuery);
        }
    }
}