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
        private readonly IServiceProvider _services;

        public OperationExecuter()
            : this(new DefaultServiceProvider())
        { }

        public OperationExecuter(IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = services;
        }

        public async Task<QueryResult> ExecuteRequestAsync(
            Schema schema, DocumentNode queryDocument, string operationName,
            Dictionary<string, IValueNode> variableValues, object initialValue,
            CancellationToken cancellationToken)
        {
            ExecutionContext executionContext = CreateExecutionContext(
                schema, queryDocument, operationName,
                variableValues, initialValue);

            switch (executionContext.Operation.Operation)
            {
                case OperationType.Query:
                    List<FieldResolverTask> tasks = CreateInitialFieldResolverBatch(
                        executionContext, schema.QueryType);
                    executionContext.NextBatch.AddRange(tasks);
                    await ExecuteFieldResolversAsync(executionContext, cancellationToken);
                    break;

                case OperationType.Mutation:
                    throw new NotSupportedException();

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
            OperationDefinitionNode operation = GetOperation(queryDocument, operationName);
            VariableCollection variables = new VariableCollection(
                _variableValueResolver.CoerceVariableValues(
                    schema, operation, vars));
            ExecutionContext executionContext = new ExecutionContext(
                schema, queryDocument, operation, variables, _services,
                initialValue, null);
            return executionContext;
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
                executionContext.FieldResolver.CollectFields(objectType,
                    executionContext.Operation.SelectionSet,
                    error => executionContext.Errors.Add(error));

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

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task ExecuteFieldResolverBatchAsync(
            ExecutionContext executionContext,
            List<FieldResolverTask> batch,
            CancellationToken cancellationToken)
        {
            List<(FieldResolverTask task, object resolverResult)> runningTasks =
                new List<(FieldResolverTask, object)>();

            foreach (FieldResolverTask task in batch)
            {
                IResolverContext resolverContext = new ResolverContext(
                    executionContext, task);
                object resolverResult = task.FieldSelection.Field.Resolver(
                    resolverContext, cancellationToken);
                runningTasks.Add((task, resolverResult));
            }

            foreach (var runningTask in runningTasks)
            {
                FieldSelection fieldSelection = runningTask.task.FieldSelection;
                object fieldValue = await CompleteFieldValueAsync(
                    runningTask.resolverResult);

                TryCompleteValue(executionContext, runningTask.task.Source,
                    fieldSelection, fieldSelection.Field.Type,
                    runningTask.task.Path, fieldValue,
                    runningTask.task.SetValue);
            }
        }

        // TODO : refactor this
        private bool TryCompleteValue(
            ExecutionContext executionContext,
            ImmutableStack<object> source,
            FieldSelection fieldSelection,
            IType fieldType,
            Path path,
            object fieldValue,
            Action<object> setValue)
        {
            object completedValue = fieldValue;

            if (fieldType.IsNonNullType())
            {
                IType innerType = fieldType.InnerType();
                if (!TryCompleteValue(
                    executionContext, source, fieldSelection,
                    innerType, path, completedValue, setValue))
                {
                    executionContext.Errors.Add(new FieldError(
                        "Cannot return null for non-nullable field.",
                        fieldSelection.Node));
                    return false;
                }
                return true;
            }

            if (completedValue == null)
            {
                setValue(null);
                return false;
            }

            if (fieldSelection.Field.Type.IsListType())
            {
                return TryCompleteListValue(executionContext, source,
                    fieldSelection, fieldType, path, completedValue, setValue);
            }

            if (fieldSelection.Field.Type.IsScalarType()
                || fieldSelection.Field.Type.IsEnumType())
            {
                return TryCompleteScalarValue(executionContext, source,
                    fieldSelection, fieldType, path, completedValue, setValue);
            }

            return TryCompleteObjectValue(executionContext, source,
                fieldSelection, fieldType, path, completedValue, setValue);
        }

        private bool TryCompleteListValue(
            ExecutionContext executionContext,
            ImmutableStack<object> source,
            FieldSelection fieldSelection,
            IType fieldType,
            Path path,
            object fieldValue,
            Action<object> setValue)
        {
            IType elementType = fieldSelection.Field.Type.ElementType();
            bool isNonNullElement = elementType.IsNonNullType();
            List<object> list = new List<object>();
            int i = 0;

            foreach (object element in ((IEnumerable)fieldValue))
            {
                Path elementPath = path.Append(i++);
                bool hasValue = TryCompleteValue(
                    executionContext, source, fieldSelection,
                    elementType, elementPath, element,
                    value => list.Add(value));

                if (isNonNullElement && !hasValue)
                {
                    executionContext.Errors.Add(new FieldError(
                        "The list does not allow null elements",
                        fieldSelection.Node));
                    setValue(null);
                    return false;
                }
            }

            setValue(list);
            return true;
        }

        private bool TryCompleteScalarValue(
            ExecutionContext executionContext,
            ImmutableStack<object> source,
            FieldSelection fieldSelection,
            IType fieldType,
            Path path,
            object fieldValue,
            Action<object> setValue)
        {
            try
            {
                // TODO :   include enums
                setValue(((ScalarType)fieldType).Serialize(fieldValue));
                return true;
            }
            catch (ArgumentException ex)
            {
                executionContext.Errors.Add(new FieldError(
                    ex.Message, fieldSelection.Node));
            }
            catch (Exception)
            {
                executionContext.Errors.Add(new FieldError(
                    "Undefined field serialization error.",
                    fieldSelection.Node));
            }

            setValue(null);
            return false;
        }

        private bool TryCompleteObjectValue(
            ExecutionContext executionContext,
            ImmutableStack<object> source,
            FieldSelection fieldSelection,
            IType fieldType,
            Path path,
            object fieldValue,
            Action<object> setValue)
        {
            ObjectType objectType = ResolveObjectType(fieldType, fieldValue);
            Dictionary<string, object> objectResult = new Dictionary<string, object>();

            IReadOnlyCollection<FieldSelection> fields = executionContext
                .FieldResolver.CollectFields(
                    objectType, fieldSelection.Node.SelectionSet,
                    executionContext.Errors.Add);

            foreach (FieldSelection field in fields)
            {
                executionContext.NextBatch.Add(new FieldResolverTask(
                    source.Push(fieldValue), objectType, field,
                    path.Append(field.ResponseName), objectResult));
            }

            setValue(objectResult);
            return true;
        }

        private ObjectType ResolveObjectType(
            IType fieldType, object fieldValue)
        {
            if (fieldType is ObjectType objectType)
            {
                return objectType;
            }
            else if (fieldType is InterfaceType interfaceType)
            {
                // TODO : Fix context issue
                return interfaceType.ResolveType(null, fieldValue);
            }
            else if (fieldType is UnionType unionType)
            {
                // TODO : Fix context issue
                return unionType.ResolveType(null, fieldValue);
            }

            // TODO : error message
            throw new NotSupportedException("we should never end up here");
        }

        // todo: rework ....
        private async Task<object> CompleteFieldValueAsync(object resolverResult)
        {
            switch (resolverResult)
            {
                case Task<object> task:
                    return await task;
                case Task<Func<object>> taskFunc:
                    return (await taskFunc)();
                case Func<Task<object>> funcTask:
                    return await funcTask();
                case Func<object> func:
                    return func();
                default:
                    return resolverResult;
            }
        }

    }
}
