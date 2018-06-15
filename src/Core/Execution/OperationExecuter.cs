using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public class OperationExecuter
    {
        private static readonly VariableValueResolver _variableValueResolver =
            new VariableValueResolver();
        private static readonly FieldValueCompleter _valueCompleter =
            new FieldValueCompleter();

        private readonly Schema _schema;
        private int _maxExecutionDepth;
        private TimeSpan _executionTimeout;

        public OperationExecuter(Schema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _maxExecutionDepth = schema.Options.MaxExecutionDepth;
            _executionTimeout = schema.Options.ExecutionTimeout;
        }

        public async Task<QueryResult> ExecuteRequestAsync(
            DocumentNode queryDocument, string operationName,
            Dictionary<string, IValueNode> variableValues,
            object initialValue, CancellationToken cancellationToken)
        {
            ExecutionContext executionContext = CreateExecutionContext(
                queryDocument, operationName, variableValues, initialValue);

            await ExecuteOperationAsync(executionContext, cancellationToken);

            if (executionContext.Errors.Any())
            {
                return new QueryResult(
                    executionContext.Data,
                    executionContext.Errors);
            }

            return new QueryResult(executionContext.Data);
        }

        private ExecutionContext CreateExecutionContext(
            DocumentNode queryDocument, string operationName,
            Dictionary<string, IValueNode> variableValues, object initialValue)
        {
            Dictionary<string, IValueNode> vars = variableValues
                ?? new Dictionary<string, IValueNode>();
            OperationDefinitionNode operation = GetOperation(
                queryDocument, operationName);
            ObjectType operationType = GetOperationType(_schema, operation);

            if (initialValue == null && _schema.TryGetNativeType(
                operationType.Name, out Type nativeType))
            {
                initialValue = _schema.GetService(nativeType);
            }

            VariableCollection variables = new VariableCollection(
                _variableValueResolver.CoerceVariableValues(
                    _schema, operation, vars));

            ExecutionContext executionContext = new ExecutionContext(
                _schema, queryDocument, operation, variables,
                initialValue, null);

            return executionContext;
        }

        private ObjectType GetOperationType(
            Schema schema, OperationDefinitionNode operation)
        {
            switch (operation.Operation)
            {
                case OperationType.Query:
                    return schema.QueryType;

                case OperationType.Mutation:
                    return schema.MutationType;

                case OperationType.Subscription:
                    return schema.SubscriptionType;

                default:
                    throw new NotSupportedException(
                        "The specified operation type is not supported.");
            }
        }

        private OperationDefinitionNode GetOperation(
            DocumentNode queryDocument, string operationName)
        {
            OperationDefinitionNode[] operations = queryDocument.Definitions
                .OfType<OperationDefinitionNode>()
                .ToArray();

            if (string.IsNullOrEmpty(operationName))
            {
                if (operations.Length == 1)
                {
                    return operations[0];
                }

                throw new QueryException(
                    "Only queries that contain one operation can be executed " +
                    "without specifying the opartion name.");
            }
            else
            {
                OperationDefinitionNode operation = operations.SingleOrDefault(
                    t => string.Equals(t.Name.Value, operationName, StringComparison.Ordinal));
                if (operation == null)
                {
                    throw new QueryException(
                        $"The specified operation `{operationName}` does not exist.");
                }
                return operation;
            }
        }

        private async Task ExecuteOperationAsync(
            ExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            CancellationTokenSource requestTimeoutCts =
                new CancellationTokenSource(_executionTimeout);

            CancellationTokenSource combinedCts = CancellationTokenSource
                .CreateLinkedTokenSource(requestTimeoutCts.Token, cancellationToken);

            try
            {
                switch (executionContext.Operation.Operation)
                {
                    case OperationType.Query:
                        AddResolverTasks(executionContext, _schema.QueryType);
                        await ExecuteFieldResolversAsync(
                            executionContext, cancellationToken);
                        break;

                    case OperationType.Mutation:
                        AddResolverTasks(executionContext, _schema.MutationType);
                        await ExecuteFieldResolversSeriallyAsync(
                            executionContext, cancellationToken);
                        break;

                    case OperationType.Subscription:
                        throw new NotSupportedException();
                }
            }
            finally
            {
                combinedCts.Dispose();
                requestTimeoutCts.Dispose();
            }
        }

        private void AddResolverTasks(
            ExecutionContext executionContext,
            ObjectType type)
        {
            List<FieldResolverTask> tasks = CreateInitialFieldResolverBatch(
                executionContext, _schema.QueryType);
            executionContext.NextBatch.AddRange(tasks);
        }

        private List<FieldResolverTask> CreateInitialFieldResolverBatch(
            ExecutionContext executionContext,
            ObjectType objectType)
        {
            ImmutableStack<object> source = ImmutableStack<object>
                .Empty.Push(executionContext.RootValue);

            List<FieldResolverTask> batch = new List<FieldResolverTask>();

            IReadOnlyCollection<FieldSelection> fieldSelections =
                executionContext.CollectFields(objectType,
                    executionContext.Operation.SelectionSet);

            foreach (FieldSelection fieldSelection in fieldSelections)
            {
                batch.Add(new FieldResolverTask(source,
                    objectType, fieldSelection,
                    Path.New(fieldSelection.ResponseName),
                    executionContext.Data));
            }

            return batch;
        }

        private async Task ExecuteFieldResolversAsync(
            ExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            while (executionContext.NextBatch.Count > 0)
            {
                List<FieldResolverTask> currentBatch =
                    new List<FieldResolverTask>(executionContext.NextBatch);
                executionContext.NextBatch.Clear();

                await ExecuteFieldResolverBatchAsync(executionContext,
                    currentBatch, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private async Task ExecuteFieldResolverBatchAsync(
            ExecutionContext executionContext,
            List<FieldResolverTask> batch,
            CancellationToken cancellationToken)
        {
            List<(FieldResolverTask task, IResolverContext context, object resolverResult)> runningTasks =
                new List<(FieldResolverTask, IResolverContext, object)>();

            foreach (FieldResolverTask task in batch)
            {
                IResolverContext resolverContext = new ResolverContext(
                        executionContext, task);

                if (task.Path.Depth <= _maxExecutionDepth)
                {
                    object resolverResult = ExecuteFieldResolver(
                        resolverContext, task.FieldSelection.Field,
                        task.FieldSelection.Node, cancellationToken);
                    runningTasks.Add((task, resolverContext, resolverResult));
                }
                else
                {
                    runningTasks.Add((task, resolverContext,
                        new FieldError(
                            $"The field has a depth of {task.Path.Depth}, " +
                            "which exceeds max allowed depth of " +
                            $"{_maxExecutionDepth}", task.FieldSelection.Node)));
                }
            }

            foreach (var runningTask in runningTasks)
            {
                object fieldValue = await HandleFieldValueAsync(
                    runningTask.resolverResult);

                FieldValueCompletionContext completionContext =
                    new FieldValueCompletionContext(
                        executionContext, runningTask.context,
                        runningTask.task.FieldSelection,
                        runningTask.task.SetValue,
                        fieldValue);
                CompleteValue(completionContext);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private async Task ExecuteFieldResolversSeriallyAsync(
            ExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            List<FieldResolverTask> currentBatch =
                new List<FieldResolverTask>(executionContext.NextBatch);
            executionContext.NextBatch.Clear();

            await ExecuteFieldResolverBatchSeriallyAsync(executionContext,
                currentBatch, cancellationToken);
        }

        private async Task ExecuteFieldResolverBatchSeriallyAsync(
            ExecutionContext executionContext,
            List<FieldResolverTask> batch,
            CancellationToken cancellationToken)
        {
            List<(FieldResolverTask task, object resolverResult)> runningTasks =
                new List<(FieldResolverTask, object)>();

            foreach (FieldResolverTask task in batch)
            {
                // execute resolver
                ResolverContext resolverContext = new ResolverContext(
                    executionContext, task);
                object resolverResult = ExecuteFieldResolver(
                    resolverContext, task.FieldSelection.Field,
                    task.FieldSelection.Node, cancellationToken);

                // handle async results
                resolverResult = await HandleFieldValueAsync(
                    resolverResult);

                FieldValueCompletionContext completionContext =
                    new FieldValueCompletionContext(
                        executionContext, resolverContext,
                        task.FieldSelection, task.SetValue,
                        resolverResult);

                CompleteValue(completionContext);

                // execute sub-selection fields normally
                await ExecuteFieldResolversAsync(executionContext, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private object ExecuteFieldResolver(
            IResolverContext resolverContext,
            Field field,
            FieldNode fieldSelection,
            CancellationToken cancellationToken)
        {
            try
            {
                return field.Resolver(resolverContext, cancellationToken);
            }
            catch (QueryException ex)
            {
                return ex.Errors;
            }
            catch (Exception)
            {
                return new FieldError("Internal resolver error.", fieldSelection);
            }
        }

        private ObjectType ResolveObjectType(
            IResolverContext context,
            IType fieldType,
            object fieldValue)
        {
            if (fieldType is ObjectType objectType)
            {
                return objectType;
            }
            else if (fieldType is InterfaceType interfaceType)
            {
                return interfaceType.ResolveType(context, fieldValue);
            }
            else if (fieldType is UnionType unionType)
            {
                return unionType.ResolveType(context, fieldValue);
            }

            // TODO : error message
            throw new NotSupportedException("we should never end up here");
        }

        // todo: rework ....
        private async Task<object> HandleFieldValueAsync(object resolverResult)
        {
            switch (resolverResult)
            {
                case Task<object> task:
                    return await HandleFieldValueTaskAsync(task);
                default:
                    return resolverResult;
            }
        }

        private async Task<object> HandleFieldValueTaskAsync(Task<object> task)
        {
            try
            {
                return await task;
            }
            catch (QueryException ex)
            {
                return ex.Errors;
            }
            catch (Exception)
            {
                return new QueryError("Internal resolver error.");
            }
        }

        private void CompleteValue(FieldValueCompletionContext completionContext)
        {
            _valueCompleter.CompleteValue(completionContext);
        }
    }
}
