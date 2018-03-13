using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prometheus.Resolvers;
using Prometheus.Abstractions;

namespace Prometheus.Execution
{
    public class OperationExecuter
        : IOperationExecuter
    {
        private readonly IServiceProvider _services;

        public OperationExecuter(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public async Task<IReadOnlyDictionary<string, object>> ExecuteAsync(
            IOptimizedOperation operation,
            IVariableCollection variables,
            object initialValue,
            CancellationToken cancellationToken)
        {
            Dictionary<string, object> response = new Dictionary<string, object>();
            HashSet<IBatchedQuery> batchedQueries = new HashSet<IBatchedQuery>();
            IList<IResolveSelectionTask> batch = CreateInitialTaskBatch(
                operation, variables, initialValue, batchedQueries, response);

            // execute operation
            while (batch.Count > 0)
            {
                // execute selection task batch
                await Task.WhenAll(batch.Select(t => t.ExecuteAsync(cancellationToken)).ToArray());

                // execute batched queries
                await Task.WhenAll(batchedQueries.Select(t => t.ExecuteAsync(cancellationToken)).ToArray());

                // collect delayed resolver results and integrate results into overall result map
                batch = FinalizeResolverResults(batch, variables);

                // clear state for next batch
                batchedQueries.Clear();
            }

            return response;
        }

        private IList<IResolveSelectionTask> CreateInitialTaskBatch(
            IOptimizedOperation operation,
            IVariableCollection variables,
            object initialValue,
            HashSet<IBatchedQuery> batchedQueries,
            Dictionary<string, object> response)
        {
            IResolverContext rootResolverContext = operation.CreateContext(
                _services, variables, q => batchedQueries.Add(q));

            List<IResolveSelectionTask> nextBatch = new List<IResolveSelectionTask>();

            foreach (IOptimizedSelection selection in operation.Selections)
            {
                IResolverContext selectionResolverContext =
                    selection.CreateContext(rootResolverContext, initialValue);
                ResolveSelectionTask task = new ResolveSelectionTask(
                    selectionResolverContext, selection,
                    r => response[selection.Name] = r);
                nextBatch.Add(task);
            }

            return nextBatch;
        }

        private IList<IResolveSelectionTask> FinalizeResolverResults(
            IEnumerable<IResolveSelectionTask> batch,
            IVariableCollection variables)
        {
            List<IResolveSelectionTask> nextBatch = new List<IResolveSelectionTask>();

            foreach (IResolveSelectionTask task in batch)
            {
                IType fieldType = task.Selection.FieldDefinition.Type;
                ISelectionResultProcessor resultProcessor =
                    SelectionResultProcessorResolver.GetProcessor(
                        task.Context.Schema, fieldType);
                nextBatch.AddRange(resultProcessor.Process(task));
            }

            return nextBatch;
        }
    }
}