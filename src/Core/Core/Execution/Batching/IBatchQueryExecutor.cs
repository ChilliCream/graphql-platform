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

        public async Task<IBatchQueryExecutionResult> ExecuteAsync(
            IReadOnlyList<IReadOnlyQueryRequest> batch,
            CancellationToken cancellationToken)
        {
            var variables = new ConcurrentBag<ExportedVariable>();
            var visitor = new CollectVariablesVisitor(Schema);
            var visitationMap = new CollectVariablesVisitationMap();

            Dictionary<string, FragmentDefinitionNode> fragments = null;
            DocumentNode previous = null;

            for (var i = 0; i < batch.Count; i++)
            {
                IReadOnlyQueryRequest request = batch[i];

                DocumentNode document = request.Query is QueryDocument d
                    ? d.Document
                    : Utf8GraphQLParser.Parse(request.Query.ToSource());

                OperationDefinitionNode operation =
                    document.GetOperation(request.OperationName);

                if (document != previous)
                {
                    fragments = document.GetFragments();
                    visitationMap.Initialize(fragments);
                }

                operation.Accept(
                    visitor,
                    visitationMap,
                    n => VisitorAction.Continue);

                previous = document;
                document = RewriteDocument(operation, fragments, visitor);

                var requestBuilder = QueryRequestBuilder.From(request);
                requestBuilder.SetQuery(document);

                IExecutionResult result =
                    (IReadOnlyQueryResult)await _executor.ExecuteAsync(
                        requestBuilder.Create(), cancellationToken);

            }

            throw new NotImplementedException();

        }

        private DocumentNode RewriteDocument(
            OperationDefinitionNode operation,
            IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
            CollectVariablesVisitor visitor)
        {
            var definitions = new List<IDefinitionNode>();

            var variables = operation.VariableDefinitions.ToList();
            variables.AddRange(visitor.VariableDeclarations);
            operation = operation.WithVariableDefinitions(variables);
            definitions.Add(operation);

            foreach (string fragmentName in visitor.TouchedFragments)
            {
                definitions.Add(fragments[fragmentName]);
            }

            return new DocumentNode(definitions);
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
        private readonly CollectVariablesVisitor _visitor;
        private readonly CollectVariablesVisitationMap _visitationMap =
            new CollectVariablesVisitationMap();

        private DocumentNode _previous;
        private Dictionary<string, FragmentDefinitionNode> _fragments;
        private int _index = 0;

        public BatchQueryExecutionResult(
            IQueryExecutor executor,
            IReadOnlyList<IReadOnlyQueryRequest> batch)
        {
            _executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            _batch = batch
                ?? throw new ArgumentNullException(nameof(batch));

            _visitor = new CollectVariablesVisitor(executor.Schema);
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
            IReadOnlyQueryRequest request = _batch[_index++];

            DocumentNode document = request.Query is QueryDocument d
                ? d.Document
                : Utf8GraphQLParser.Parse(request.Query.ToSource());

            OperationDefinitionNode operation =
                document.GetOperation(request.OperationName);

            if (document != _previous)
            {
                _fragments = document.GetFragments();
                _visitationMap.Initialize(_fragments);
            }

            operation.Accept(
                _visitor,
                _visitationMap,
                n => VisitorAction.Continue);

            _previous = document;

            request = QueryRequestBuilder.From(request)
                .SetQuery(
                    RewriteDocument(operation))
                .SetVariableValues(
                    MergeVariables(request.VariableValues, operation))
                .Create();

            var result =
                (IReadOnlyQueryResult)await _executor.ExecuteAsync(
                    request, cancellationToken);
            IsCompleted = _index >= _batch.Count;
            return result;
        }

        private DocumentNode RewriteDocument(
            OperationDefinitionNode operation)
        {
            var definitions = new List<IDefinitionNode>();

            var variables = operation.VariableDefinitions.ToList();
            variables.AddRange(_visitor.VariableDeclarations);
            operation = operation.WithVariableDefinitions(variables);
            definitions.Add(operation);

            foreach (string fragmentName in _visitor.TouchedFragments)
            {
                definitions.Add(_fragments[fragmentName]);
            }

            return new DocumentNode(definitions);
        }

        private IReadOnlyDictionary<string, object> MergeVariables(
            IReadOnlyDictionary<string, object> variables,
            OperationDefinitionNode operation)
        {

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
