using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Execution.Batching
{
    public interface IBatchQueryExecutor
        : IDisposable
    {
        ISchema Schema { get; }

        Task<IBatchQueryExecutionResult> ExecuteAsync(
            IReadOnlyList<IReadOnlyQueryRequest> batch,
            CancellationToken cancellationToken);
    }

    public class BatchQueryExecutor
        : IBatchQueryExecutor
    {
        private readonly IQueryExecutor _executor;

        public BatchQueryExecutor(IQueryExecutor executor)
        {
            _executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
        }

        public ISchema Schema => _executor.Schema;

        public Task<IBatchQueryExecutionResult> ExecuteAsync(
            IReadOnlyList<IReadOnlyQueryRequest> batch,
            CancellationToken cancellationToken)
        {


            for (int i = 0; i < batch.Count; i++)
            {



            }
        }





        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public class BatchQueryExecutionResult
        : IBatchQueryExecutionResult
    {
        private readonly ThreadLocal<CollectVariablesVisitor> _collectVars =
            new ThreadLocal<CollectVariablesVisitor>();
        private readonly IQueryExecutor _executor;
        private readonly IReadOnlyList<IReadOnlyQueryRequest> _batch;
        private readonly ConcurrentBag<ExportedVariable> _exportedVariables =
            new ConcurrentBag<ExportedVariable>();
        private int _index = 0;

        public BatchQueryExecutionResult(
            IQueryExecutor executor,
            IReadOnlyList<IReadOnlyQueryRequest> batch)
        {
            _executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            _batch = batch
                ?? throw new ArgumentNullException(nameof(batch));
        }

        public IReadOnlyCollection<IError> Errors { get; }

        public IReadOnlyDictionary<string, object> Extensions { get; }

        public IReadOnlyDictionary<string, object> ContextData { get; }

        public bool IsCompleted { get; private set; }

        public Task<IReadOnlyQueryResult> ReadAsync() =>
            ReadAsync(CancellationToken.None);

        public async Task<IReadOnlyQueryResult> ReadAsync(
            CancellationToken cancellationToken)
        {
            IReadOnlyQueryRequest request = CreateNextRequest();
            var result = (IReadOnlyQueryResult)await _executor
                .ExecuteAsync(request, cancellationToken)
                .ConfigureAwait(false);
            IsCompleted = _index >= _batch.Count;
            return result;
        }

        private IReadOnlyQueryRequest CreateNextRequest()
        {
            IReadOnlyQueryRequest request = _batch[_index];

            DocumentNode document = request.Query is QueryDocument d
                ? d.Document
                : Utf8GraphQLParser.Parse(request.Query.ToSource());

            var visitor = _collectVars.Value;
            visitor.Prepare(request.OperationName);
            document.Accept(visitor);

            HashSet<string> _names =

            IDefinitionNode[] definitions = document.Definitions.ToArray();
            for (var i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] is OperationDefinitionNode op
                    && (request.OperationName == null
                        || request.OperationName.Equals(
                            op.Name.Value,
                            StringComparison.Ordinal)))
                {
                    

                    break;
                }
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
