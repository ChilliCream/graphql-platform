using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    public class OptimizedOperation
        : IOptimizedOperation
    {
        private readonly object _sync = new object();
        private readonly OperationContext _operationContext;
        private readonly OptimizedSelectionHelper _selectionHelper;
        private ImmutableList<IOptimizedSelection> _selections;
        private bool _isInitialized;

        public OptimizedOperation(OperationContext operationContext)
        {
            _operationContext = operationContext
                ?? throw new ArgumentNullException(nameof(operationContext));

            _selectionHelper = new OptimizedSelectionHelper(
                operationContext,
                operationContext.Operation.Type.ToString());
        }

        public ISchema Schema => _operationContext.Schema;

        public QueryDocument QueryDocument => _operationContext.QueryDocument;

        public OperationDefinition Operation => _operationContext.Operation;

        public IReadOnlyCollection<IOptimizedSelection> Selections
        {
            get
            {
                if (!_isInitialized)
                {
                    lock (_sync)
                    {
                        if (!_isInitialized)
                        {
                            _selections = ResolveFields().ToImmutableList();
                            _isInitialized = true;
                        }
                    }
                }
                return _selections;
            }
        }

        public IResolverContext CreateContext(
            IServiceProvider services,
            IVariableCollection variables,
            Action<IBatchedQuery> registerQuery)
        {
            if (registerQuery == null)
            {
                throw new ArgumentNullException(nameof(registerQuery));
            }

            return ResolverContext.Create(
                services, _operationContext,
                variables, registerQuery);
        }

        private IEnumerable<IOptimizedSelection> ResolveFields()
        {
            foreach (Field field in _operationContext.Operation.SelectionSet.OfType<Field>())
            {
                if (_selectionHelper.TryCreateSelectionContext(field, out var sc))
                {
                    yield return new OptimizedSelection(_operationContext, sc);
                }
            }
        }
    }
}