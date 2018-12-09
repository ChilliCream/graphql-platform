using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal class OperationExecuter
    {
        private static readonly Dictionary<OperationType, IExecutionStrategy> _executionStrategy =
            new Dictionary<OperationType, IExecutionStrategy>
            {
                { OperationType.Query, new QueryExecutionStrategy() },
                { OperationType.Mutation, new MutationExecutionStrategy() },
                { OperationType.Subscription, new SubscriptionExecutionStrategy() }
            };

        private readonly ISchema _schema;
        private readonly DocumentNode _queryDocument;
        private readonly OperationDefinitionNode _operation;
        private readonly DirectiveLookup _directiveLookup;
        private readonly TimeSpan _executionTimeout;
        private readonly VariableValueBuilder _variableValueBuilder;
        private readonly IExecutionStrategy _strategy;

        public OperationExecuter(
            ISchema schema,
            DocumentNode queryDocument,
            OperationDefinitionNode operation)
        {
            _schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            _queryDocument = queryDocument
                ?? throw new ArgumentNullException(nameof(queryDocument));
            _operation = operation
                ?? throw new ArgumentNullException(nameof(operation));

            _executionTimeout = schema.Options.ExecutionTimeout;

            _variableValueBuilder = new VariableValueBuilder(schema, _operation);

            if (!_executionStrategy.TryGetValue(_operation.Operation,
                out IExecutionStrategy strategy))
            {
                throw new NotSupportedException();
            }
            _strategy = strategy;

            var directiveCollector = new DirectiveCollector(_schema);
            directiveCollector.VisitDocument(_queryDocument);
            _directiveLookup = directiveCollector.CreateLookup();
        }

        public async Task<IExecutionResult> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken)
        {
            var requestTimeoutCts =
                new CancellationTokenSource(_executionTimeout);

            var combinedCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    requestTimeoutCts.Token, cancellationToken);

            IExecutionContext executionContext =
                CreateExecutionContext(
                    request,
                    cancellationToken);

            try
            {
                return await _strategy.ExecuteAsync(
                    executionContext, combinedCts.Token);
            }
            finally
            {
                executionContext.Dispose();
                combinedCts.Dispose();
                requestTimeoutCts.Dispose();
            }
        }

        private IExecutionContext CreateExecutionContext(
            OperationRequest request,
            CancellationToken cancellationToken)
        {
            VariableCollection variables = _variableValueBuilder
                .CreateValues(request.VariableValues);

            var executionContext = new ExecutionContext(
                _schema, _directiveLookup, _queryDocument,
                _operation, request, variables,
                cancellationToken);

            return executionContext;
        }
    }

    public class Operation
    {
        public Operation(ISchema schema, DocumentNode queryDocument, OperationDefinitionNode operation)
        {
            this.Schema = schema;
            this.QueryDocument = queryDocument;
            this.Operation = operation;

        }
        public ISchema Schema { get; }
        public DocumentNode QueryDocument { get; }
        public OperationDefinitionNode Operation { get; }
    }

    public delegate IReadOnlyDictionary<TKey, TValue> FetchData<TKey, TValue>(
        IEnumerable<TKey> keys);

    public delegate ILookup<TKey, TValue> FetchGroupData<TKey, TValue>(
        IEnumerable<TKey> keys);

    public interface IDataLoaderSession
    {
        object LoadAsync<TKey, TValue>(string key, FetchData<TKey, TValue> fetchData);

        ILookup<string, object> s





        IDataLoader = ctx.DataLoader<int, ICharacter>("foo", ctx.Service().GetCharacters));

IDataLoader = ctx.DataLoader<int, ICharacter>(ctx.Service().GetCharacters));

public User Resolver([DataLoader("key")]IDataLoader loader)

// schema data loader
        public User Resolver([DataLoader("key")]Func<int, Task> loadCharacter)

public Task Resolver(IResolverContext context, IRepository repository, int userId)
        {
            // adhoc
            IDataLoader loader = context.DataLoader<int, ICharacter>("foo", repository.GetCharacters));
            return loader.LoadAsync(userId);
        }
    }
}
