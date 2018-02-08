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
            IList<IResolveSelectionTask> batch = null; // todo: create inital results
            HashSet<IBatchedQuery> batchedQueries = new HashSet<IBatchedQuery>();

            // execute operation
            while (batch != null)
            {
                // execute selection task batch
                await Task.WhenAll(batch.Select(t => t.ExecuteAsync(cancellationToken)).ToArray());

                // execute batched queries
                await Task.WhenAll(batchedQueries.Select(t => t.ExecuteAsync(cancellationToken)).ToArray());

                // collect delayed resolver and integrate result int overall result map
                batch = FinalizeResolverResults(batch);

                // clear state for next batch
                batchedQueries.Clear();
            }
        }

        private IList<IResolveSelectionTask> CreateInitialTaskBatch(
            IOptimizedOperation operation,
            IVariableCollection variables,
            object initialValue)
        {
            List<IResolveSelectionTask> nextBatch = new List<IResolveSelectionTask>();



            foreach (IOptimizedSelection selection in operation.Selections)
            {

            }


            return nextBatch;
        }

        private IList<IResolveSelectionTask> FinalizeResolverResults(IEnumerable<IResolveSelectionTask> batch)
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
        void FinalizeResult();
    }

    internal class ResolveSelectionTask
        : IResolveSelectionTask
    {
        public IResolverContext Context => throw new NotImplementedException();

        public IOptimizedSelection Selection => throw new NotImplementedException();

        public object Result => throw new NotImplementedException();

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void FinalizeResult()
        {
            throw new NotImplementedException();
        }
    }
}