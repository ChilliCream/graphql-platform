using System;
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
        private readonly IServiceProvider _services;

        public OperationExecuter(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public async Task<IDictionary<string, object>> ExecuteAsync(
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
            while (batch != null)
            {
                // execute selection task batch
                await Task.WhenAll(batch.Select(t => t.ExecuteAsync(cancellationToken)).ToArray());

                // execute batched queries
                await Task.WhenAll(batchedQueries.Select(t => t.ExecuteAsync(cancellationToken)).ToArray());

                // collect delayed resolver and integrate result int overall result map
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
                ISelectionResultProcessor resultProcessor =
                    GetSelectionResultProcessor(task.Selection.FieldDefinition.Type);
                nextBatch.AddRange(resultProcessor.Process(task));
            }

            return nextBatch;
        }

        private ISelectionResultProcessor GetSelectionResultProcessor(IType fieldType)
        {
            throw new NotImplementedException();
        }
    }

    internal interface ISelectionResultProcessor
    {
        // in: the executed selection result that contains the computed result of the selection.
        // out: in case the selection is a list or object we will return new selection tasks that have to be executed.
        IEnumerable<IResolveSelectionTask> Process(IResolveSelectionTask selectionTask);
    }

    


    internal interface IResolveSelectionTask
    {
        IResolverContext Context { get; }

        IOptimizedSelection Selection { get; }

        object Result { get; }

        Task ExecuteAsync(CancellationToken cancellationToken);

        void IntegrateResult(object result);
    }


    internal class ResolveSelectionTask
        : IResolveSelectionTask
    {
        private object _result;
        private readonly Action<object> _addValueToResultMap;

        public ResolveSelectionTask(
            IResolverContext context,
            IOptimizedSelection selection,
            Action<object> addValueToResultMap)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _addValueToResultMap = addValueToResultMap ?? throw new ArgumentNullException(nameof(addValueToResultMap));
        }

        public IResolverContext Context { get; }

        public IOptimizedSelection Selection { get; }

        public object Result => _result;

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _result = await Selection.Resolver.ResolveAsync(Context, cancellationToken);
        }

        public void IntegrateResult(object finalResult)
        {
            _addValueToResultMap(finalResult);
        }
    }
}