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
    internal class OperationRequest
    {
        private static readonly FieldValueCompleter _valueCompleter =
            new FieldValueCompleter();

        private readonly Schema _schema;
        private readonly DocumentNode _queryDocument;
        private readonly OperationDefinitionNode _operation;
        private readonly ObjectType _operationType;
        private readonly int _maxExecutionDepth;
        private readonly TimeSpan _executionTimeout;
        private readonly VariableValueBuilder _variableValueBuilder;

        public OperationRequest(Schema schema,
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

            _operationType = schema.GetOperationType(_operation.Operation);
            _variableValueBuilder = new VariableValueBuilder(schema, _operation);
        }

        public async Task<QueryResult> ExecuteAsync(
            IReadOnlyDictionary<string, IValueNode> variableValues = null,
            object initialValue = null,
            CancellationToken cancellationToken = default)
        {
            ExecutionContext executionContext = CreateExecutionContext(
                variableValues, initialValue);

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
            IReadOnlyDictionary<string, IValueNode> variableValues,
            object initialValue)
        {
            VariableCollection variables = _variableValueBuilder
                .CreateValues(variableValues);

            object rootValue = ResolveRootValue(initialValue);

            ExecutionContext executionContext = new ExecutionContext(
                _schema, _queryDocument, _operation, variables,
                rootValue, null);

            return executionContext;
        }

        private object ResolveRootValue(
            object initialValue)
        {
            if (initialValue == null && _schema.TryGetNativeType(
               _operationType.Name, out Type nativeType))
            {
                initialValue = _schema.GetService(nativeType)
                    ?? Activator.CreateInstance(nativeType);
            }
            return initialValue;
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
                executionContext, type);
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
            catch (Exception ex)
            {
                return _schema.CreateErrorFromException(ex, fieldSelection);
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
            catch (Exception ex)
            {
                return _schema.CreateErrorFromException(ex);
            }
        }

        private void CompleteValue(FieldValueCompletionContext completionContext)
        {
            _valueCompleter.CompleteValue(completionContext);
        }
    }
}
