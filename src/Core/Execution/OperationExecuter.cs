using System;
using System.Collections;
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

        public async Task<QueryResult> ExecuteRequestAsync(
            Schema schema, DocumentNode queryDocument, string operationName,
            Dictionary<string, IValueNode> variableValues, object initialValue,
            CancellationToken cancellationToken)
        {
            ExecutionContext executionContext = CreateExecutionContext(
                schema, queryDocument, operationName,
                variableValues, initialValue);
            List<FieldResolverTask> tasks = null;

            switch (executionContext.Operation.Operation)
            {
                case OperationType.Query:
                    tasks = CreateInitialFieldResolverBatch(
                        executionContext, schema.QueryType);
                    executionContext.NextBatch.AddRange(tasks);
                    await ExecuteFieldResolversAsync(
                        executionContext, cancellationToken);
                    break;

                case OperationType.Mutation:
                    tasks = CreateInitialFieldResolverBatch(
                        executionContext, schema.MutationType);
                    executionContext.NextBatch.AddRange(tasks);
                    await ExecuteFieldResolversSeriallyAsync(
                        executionContext, cancellationToken);
                    break;

                case OperationType.Subscription:
                    throw new NotSupportedException();
            }

            if (executionContext.Errors.Any())
            {
                return new QueryResult(executionContext.Data, executionContext.Errors);
            }
            return new QueryResult(executionContext.Data);
        }

        private ExecutionContext CreateExecutionContext(
            Schema schema, DocumentNode queryDocument, string operationName,
            Dictionary<string, IValueNode> variableValues, object initialValue)
        {
            Dictionary<string, IValueNode> vars = variableValues
                ?? new Dictionary<string, IValueNode>();
            OperationDefinitionNode operation = GetOperation(
                queryDocument, operationName);
            ObjectType operationType = GetOperationType(schema, operation);

            if (initialValue == null && schema.TryGetNativeType(
                operationType.Name, out Type nativeType))
            {
                initialValue = schema.Services.GetService(nativeType);
            }

            VariableCollection variables = new VariableCollection(
                _variableValueResolver.CoerceVariableValues(
                    schema, operation, vars));

            ExecutionContext executionContext = new ExecutionContext(
                schema, queryDocument, operation, variables, schema.Services,
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

                // TODO : Exception
                throw new Exception();
            }
            else
            {
                OperationDefinitionNode operation = operations.SingleOrDefault(
                    t => string.Equals(t.Name.Value, operationName, StringComparison.Ordinal));
                if (operation == null)
                {
                    // TODO : Exception
                    throw new Exception();
                }
                return operation;
            }
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
                object resolverResult = ExecuteFieldResolver(
                    resolverContext, task.FieldSelection.Field,
                    task.FieldSelection.Node, cancellationToken);
                runningTasks.Add((task, resolverContext, resolverResult));
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

    internal readonly struct FieldValueCompletionContext
    {
        private readonly Action<object> _setResult;

        public FieldValueCompletionContext(
            ExecutionContext executionContext,
            IResolverContext resolverContext,
            FieldSelection selection,
            Action<object> setResult,
            object value)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (resolverContext == null)
            {
                throw new ArgumentNullException(nameof(resolverContext));
            }

            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            if (setResult == null)
            {
                throw new ArgumentNullException(nameof(setResult));
            }

            _setResult = setResult;

            ExecutionContext = executionContext;
            ResolverContext = resolverContext;
            Source = resolverContext.Source;
            Selection = selection;
            SelectionSet = selection.Node.SelectionSet;
            Type = resolverContext.Field.Type;
            Path = resolverContext.Path;
            Value = value;
            IsNullable = true;
        }

        private FieldValueCompletionContext(
            FieldValueCompletionContext context,
            IType type, bool isNullable)
        {
            _setResult = context._setResult;
            ExecutionContext = context.ExecutionContext;
            ResolverContext = context.ResolverContext;
            Source = context.Source;
            Selection = context.Selection;
            SelectionSet = context.SelectionSet;
            Path = context.Path;
            Value = context.Value;

            Type = type;
            IsNullable = isNullable;
        }

        private FieldValueCompletionContext(
            FieldValueCompletionContext context,
            Path elementPath, IType elementType,
            object element, Action<object> addElementToList)
        {
            ExecutionContext = context.ExecutionContext;
            ResolverContext = context.ResolverContext;
            Source = context.Source;
            Selection = context.Selection;
            SelectionSet = context.SelectionSet;
            IsNullable = context.IsNullable;

            Path = elementPath;
            Type = elementType;
            Value = element;
            _setResult = addElementToList;
        }

        public ExecutionContext ExecutionContext { get; }
        public IResolverContext ResolverContext { get; }
        public ImmutableStack<object> Source { get; }
        public FieldSelection Selection { get; }
        public SelectionSetNode SelectionSet { get; }
        public IType Type { get; }
        public Path Path { get; }
        public object Value { get; }
        public bool IsNullable { get; }

        public void AddErrors(IEnumerable<IQueryError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            ExecutionContext.Errors.AddRange(errors);
            _setResult(null);
        }

        public void AddError(IQueryError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            ExecutionContext.Errors.Add(error);
            _setResult(null);
        }

        public void AddError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            ExecutionContext.Errors.Add(new FieldError(message, Selection.Node));
            _setResult(null);
        }

        private void AddNonNullError()
        {
            AddError(new FieldError(
                "Cannot return null for non-nullable field.",
                Selection.Node));
        }

        public void SetResult(object value)
        {
            _setResult(value);
            if (!IsNullable && value == null)
            {
                AddNonNullError();
            }
        }

        public FieldValueCompletionContext AsNonNullValueContext()
        {
            if (Type.IsNonNullType())
            {
                return new FieldValueCompletionContext(this, Type.InnerType(), true);
            }

            throw new InvalidOperationException(
                "The current type is not a non-null type.");
        }

        public FieldValueCompletionContext AsElementValueContext(
            Path elementPath, IType elementType,
            object element, Action<object> addElementToList)
        {
            if (elementPath == null)
            {
                throw new ArgumentNullException(nameof(elementPath));
            }

            if (elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            if (addElementToList == null)
            {
                throw new ArgumentNullException(nameof(addElementToList));
            }

            return new FieldValueCompletionContext(
                this, elementPath, elementType, element, addElementToList);
        }
    }

    internal interface IFieldValueHandler
    {
        void CompleteValue(
            FieldValueCompletionContext context,
            Action<FieldValueCompletionContext> nextHandler);
    }


    internal class FieldValueCompleter
    {
        private static readonly IFieldValueHandler[] _handlers = new IFieldValueHandler[]
        {
            new QueryErrorFieldValueHandler(),
            new NonNullFieldValueHandler(),
            new NullFieldValueHandler(),
            new ListFieldValueHandler(),
            new ScalarFieldValueHandler(),
            new ObjectFieldValueHandler()
        };

        private readonly Action<FieldValueCompletionContext> _completeValue;

        public FieldValueCompleter()
        {
            Action<FieldValueCompletionContext> completeValue = null;
            for (int i = _handlers.Length - 1; i >= 0; i--)
            {
                completeValue = CreateValueCompleter(_handlers[i], completeValue);
            }
            _completeValue = completeValue;
        }

        private static Action<FieldValueCompletionContext> CreateValueCompleter(
            IFieldValueHandler handler,
            Action<FieldValueCompletionContext> completeValue)
        {
            return c => handler.CompleteValue(c, completeValue);
        }

        public void CompleteValue(FieldValueCompletionContext context)
        {
            _completeValue(context);
        }
    }

    internal class NonNullFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            FieldValueCompletionContext context,
            Action<FieldValueCompletionContext> nextHandler)
        {
            if (context.Type.IsNonNullType())
            {
                nextHandler?.Invoke(context.AsNonNullValueContext());
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }
    }

    internal class NullFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            FieldValueCompletionContext context,
            Action<FieldValueCompletionContext> nextHandler)
        {
            if (context.Value == null)
            {
                context.SetResult(null);
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }
    }

    internal class QueryErrorFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            FieldValueCompletionContext context,
            Action<FieldValueCompletionContext> nextHandler)
        {
            if (context.Value is IQueryError error)
            {
                context.AddError(error);
            }
            else if (context.Value is IEnumerable<IQueryError> errors)
            {
                context.AddErrors(errors);
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }
    }

    internal class ListFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            FieldValueCompletionContext context,
            Action<FieldValueCompletionContext> nextHandler)
        {
            if (context.Type.IsListType())
            {
                int i = 0;
                IType elementType = context.Type.ElementType();
                bool isNonNullElement = elementType.IsNonNullType();
                elementType = elementType.InnerType();
                List<object> list = new List<object>();

                if (context.Value is IEnumerable enumerable)
                {
                    foreach (object element in enumerable)
                    {
                        if (isNonNullElement && element == null)
                        {
                            context.AddError(
                                "The list does not allow null elements");
                            return;
                        }

                        nextHandler?.Invoke(context.AsElementValueContext(
                            context.Path.Append(i++), elementType,
                            element, item => list.Add(item)));
                    }
                    context.SetResult(list);
                }
                else
                {
                    context.AddError(
                        "A list value must implement " +
                        $"{typeof(IEnumerable).FullName}.");
                }
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }
    }

    internal class ScalarFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            FieldValueCompletionContext context,
            Action<FieldValueCompletionContext> nextHandler)
        {
            if (context.Type.IsScalarType() || context.Type.IsEnumType())
            {
                if (context.Type is ISerializableType serializable)
                {
                    try
                    {
                        context.SetResult(serializable.Serialize(context.Value));
                    }
                    catch (ArgumentException ex)
                    {
                        context.AddError(ex.Message);
                    }
                    catch (Exception)
                    {
                        context.AddError(
                            "Undefined field serialization error.");
                    }
                }
                else
                {
                    context.AddError(
                        "Scalar types and enum types must be serializable.");
                }
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }
    }

    internal class ObjectFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            FieldValueCompletionContext context,
            Action<FieldValueCompletionContext> nextHandler)
        {
            if (context.Type.IsObjectType()
                || context.Type.IsInterfaceType()
                || context.Type.IsUnionType())
            {
                ObjectType objectType = ResolveObjectType(
                    context.ResolverContext, context.Type, context.Value);

                OrderedDictionary objectResult = new OrderedDictionary();
                context.SetResult(objectResult);

                IReadOnlyCollection<FieldSelection> fields = context.ExecutionContext
                    .CollectFields(objectType, context.SelectionSet);

                foreach (FieldSelection field in fields)
                {
                    context.ExecutionContext.NextBatch.Add(new FieldResolverTask(
                        context.Source.Push(context.Value), objectType, field,
                        context.Path.Append(field.ResponseName), objectResult));
                }
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }

        private ObjectType ResolveObjectType(
            IResolverContext context,
            IType type, object value)
        {
            if (type is ObjectType objectType)
            {
                return objectType;
            }
            else if (type is InterfaceType interfaceType)
            {
                return interfaceType.ResolveType(context, value);
            }
            else if (type is UnionType unionType)
            {
                return unionType.ResolveType(context, value);
            }

            throw new NotSupportedException(
                "The specified type is not supported.");
        }
    }
}
