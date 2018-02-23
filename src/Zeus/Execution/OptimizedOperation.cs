using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    public class OptimizedOperation
        : IOptimizedNode
        , IOptimizedOperation
    {
        private readonly OperationContext _operationContext;
        private ImmutableList<IOptimizedSelection> _selections;

        public OptimizedOperation(
            ISchema schema,
            QueryDocument queryDocument,
            OperationDefinition operation)
        {
            _operationContext = new OperationContext(
                schema, queryDocument, operation);
            _selections = ImmutableList<IOptimizedSelection>.Empty;
        }

        public OptimizedOperation(
            ISchema schema,
            QueryDocument queryDocument,
            OperationDefinition operation,
            IEnumerable<IOptimizedSelection> selections)
        {
            _operationContext = new OperationContext(
                schema, queryDocument, operation);
            _selections = selections == null
                ? ImmutableList<IOptimizedSelection>.Empty
                : selections.ToImmutableList();
        }

        private OptimizedOperation(
            OperationContext operationContext,
            ImmutableList<IOptimizedSelection> selections)
        {
            _operationContext = operationContext;
            _selections = selections;
        }

        public ISchema Schema => _operationContext.Schema;

        public QueryDocument QueryDocument => _operationContext.QueryDocument;

        public OperationDefinition Operation => _operationContext.Operation;

        public IReadOnlyCollection<IOptimizedSelection> Selections { get; }

        IOptimizedNode IOptimizedNode.Parent => null;

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

        public OptimizedOperation AddSelections(IEnumerable<IOptimizedSelection> selections)
        {
            return new OptimizedOperation(_operationContext, _selections.AddRange(selections));
        }

        public OptimizedOperation ReplaceSelection(IOptimizedSelection oldSelection, IOptimizedSelection newSelection)
        {
            return new OptimizedOperation(_operationContext, _selections.Replace(oldSelection, newSelection));
        }

        IOptimizedNode IOptimizedNode.AddSelections(IEnumerable<IOptimizedSelection> selections)
        {
            return AddSelections(selections);
        }

        IOptimizedNode IOptimizedNode.ReplaceSelection(IOptimizedSelection oldSelection, IOptimizedSelection newSelection)
        {
            return ReplaceSelection(oldSelection, newSelection);
        }
    }
}