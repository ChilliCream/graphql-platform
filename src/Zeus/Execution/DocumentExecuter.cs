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
            Dictionary<string, object> response = new Dictionary<string, object>();

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

        private IList<IResolveSelectionTask> FinalizeResolverResults(
            IEnumerable<IResolveSelectionTask> batch,
            IVariableCollection variables)
        {
            List<IResolveSelectionTask> nextBatch = new List<IResolveSelectionTask>();

            foreach (IResolveSelectionTask task in batch)
            {
                ISelectionResultProcessor resultProcessor =
                    GetSelectionResultProcessor(task.Selection.FieldDefinition.Type);
                nextBatch.AddRange(resultProcessor.Process(task, variables));
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
        IEnumerable<IResolveSelectionTask> Process(IResolveSelectionTask selectionTask, IVariableCollection variables);
    }

    internal class ObjectSelectionResultProcessor
        : ISelectionResultProcessor
    {



        public IEnumerable<IResolveSelectionTask> Process(IResolveSelectionTask selectionTask, IVariableCollection variables)
        {
            foreach (IOptimizedSelection selection in selectionTask.Selection.Selections)
            {
                IResolverContext context = selection.CreateContext(selectionTask.Result, selectionTask.Context, variables);
            }
        }
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

        public void FinalizeResult()
        {
            if (_result is Func<object> f)
            {
                _result = f();
            }
        }

        public void Integrate(object finalResult)
        {
            _addValueToResultMap(finalResult);
        }
    }
}