using System;
using System.Collections.Generic;
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
                { OperationType.Mutation, new MutationExecutionStrategy() }
            };

        private static readonly FieldValueCompleter _valueCompleter =
            new FieldValueCompleter();

        private readonly ISchema _schema;
        private readonly DocumentNode _queryDocument;
        private readonly OperationDefinitionNode _operation;
        private readonly int _maxExecutionDepth;
        private readonly TimeSpan _executionTimeout;
        private readonly VariableValueBuilder _variableValueBuilder;
        private IExecutionStrategy _strategy;

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

            _maxExecutionDepth = schema.Options.MaxExecutionDepth;
            _executionTimeout = schema.Options.ExecutionTimeout;

            _variableValueBuilder = new VariableValueBuilder(schema, _operation);

            if (!_executionStrategy.TryGetValue(_operation.Operation,
                out IExecutionStrategy strategy))
            {
                throw new NotSupportedException();
            }
            _strategy = strategy;
        }

        public async Task<IExecutionResult> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken)
        {
            var requestTimeoutCts =
                new CancellationTokenSource(_executionTimeout);

            CancellationTokenSource combinedCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    requestTimeoutCts.Token, cancellationToken);

            try
            {
                IExecutionContext executionContext =
                    CreateExecutionContext(request);

                return await _strategy.ExecuteAsync(
                    executionContext, combinedCts.Token);
            }
            finally
            {
                combinedCts.Dispose();
                requestTimeoutCts.Dispose();
            }
        }

        private IExecutionContext CreateExecutionContext(
            OperationRequest request)
        {
            VariableCollection variables = _variableValueBuilder
                .CreateValues(request.VariableValues);

            ExecutionContext executionContext = new ExecutionContext(
                _schema, _queryDocument, _operation, request, variables);

            return executionContext;
        }
    }
}
