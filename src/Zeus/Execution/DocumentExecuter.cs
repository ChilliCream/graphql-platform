using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using GraphQLParser.AST;
using Zeus.Resolvers;
using Zeus.Abstractions;

namespace Zeus.Execution
{
    public class DocumentExecuter
        : IDocumentExecuter
    {
        private readonly IOperationOptimizer _operationOptimizer;


        public DocumentExecuter(IOperationOptimizer operationOptimizer)
        {
            _operationOptimizer = operationOptimizer;
        }

        public async Task<IDictionary<string, object>> ExecuteAsync(
            ISchema schema, QueryDocument document,
            string operationName, IDictionary<string, object> variables,
            object initialValue, CancellationToken cancellationToken)
        {


        }


    }


    public interface IOperationExecuter
    {
        Task<IDictionary<string, object>> ExecuteAsync(
           IOptimizedOperation operation,
           IVariableCollection variables,
           object initialValue,
           CancellationToken cancellationToken);
    }

    public class OperationExecuter
        : IOperationExecuter
    {
        public async Task<IDictionary<string, object>> ExecuteAsync(
            IOptimizedOperation operation,
            IVariableCollection variables,
            object initialValue,
            CancellationToken cancellationToken)
        {
            SelectionResult[] current = null; // todo: create inital results
            HashSet<IBatchedQuery> batchedQueries = new HashSet<IBatchedQuery>();
            Dictionary<IOptimizedSelection, object> resolverResults = new Dictionary<IOptimizedSelection, object>();

            // execute operation
            while (current != null)
            {
                Task<SelectionResult>[] tasks = current.Select(t =>
                    ExecuteSelectionAsync(t.Selection, variables, t.Result, t.Context, cancellationToken))
                    .ToArray();

                await Task.WhenAll(tasks);
                await ExecuteBatchedQueriesAsync(batchedQueries, cancellationToken);

                current = AddResults(tasks.Select(t => t.Result), resolverResults).ToArray();
            }

            // build result map


        }

        private void BuildResultMap(IOptimizedOperation operation, List<SelectionResult> resolverResults)
        {
            Dictionary<string, object> map = new Dictionary<string, object>();
            Queue<BuildResultMapItem> queue = new Queue<BuildResultMapItem>();
            while (queue.Any())
            {
                BuildResultMapItem current = queue.Dequeue();
                current.
            }



        }

        private void IntegrateSelectionResult(BuildResultMapItem current,  ILookup<IOptimizedSelection, SelectionResult> resultLookup)
        {



        }

        private void c(IEnumerable<IOptimizedSelection> selections, Dictionary<string, object> map, Queue<BuildResultMapItem> queue)
        {
            foreach (IOptimizedSelection selection in selections)
            {
                sele
            }
        }

        private async Task<SelectionResult> ExecuteSelectionAsync(
            IOptimizedSelection selection,
            IVariableCollection variables,
            object parentResult,
            IResolverContext parentContext,
            CancellationToken cancellationToken)
        {
            IResolverContext context = selection.CreateContext(parentResult, parentContext, variables);
            object result = selection.Resolver.ResolveAsync(context, cancellationToken);
            return new SelectionResult(selection, context, result);
        }

        private Task ExecuteBatchedQueriesAsync(ICollection<IBatchedQuery> batchedQueries, CancellationToken cancellationToken)
        {
            return Task.WhenAll(batchedQueries.Select(t => t.ExecuteAsync(cancellationToken)));
        }

        private IEnumerable<SelectionResult> AddResults(
            IEnumerable<SelectionResult> selectionResults,
            List<SelectionResult> results)
        {
            foreach (SelectionResult result in selectionResults)
            {
                // invoke delayed results
                object r = result.Result;
                if (r is Func<object> f)
                {
                    r = f();
                }





                if (result.Selection.FieldDefinition.Type.IsListType()
                    && !result.Selection.FieldDefinition.Type.ElementType().IsScalarType()
                    && r is IEnumerable l)
                {
                    foreach (object o in l)
                    {
                        yield return new SelectionResult(result.Selection, result.Context, o);
                    }
                }
            }
        }

        private class SelectionResult
        {
            public SelectionResult(
                IOptimizedSelection selection,
                IResolverContext context,
                object result)
            {
                Selection = selection;
                Context = context;
                Result = result;
            }

            public IOptimizedSelection Selection { get; }
            public IResolverContext Context { get; }
            public object Result { get; }
        }

        private class BuildResultMapItem
        {
            public BuildResultMapItem(IOptimizedSelection selection, Dictionary<string, object> map)
            {
                Selection = Selection;
                Map = map;
            }

            public IOptimizedSelection Selection { get; }
            public Dictionary<string, object> Map { get; }
        }
    }
}