using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public class QueryDocumentExecuter
    {

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
                object resolverResult = task.Field.Field.Resolver(
                    resolverContext, cancellationToken);
                runningTasks.Add((task, resolverResult));
            }

            foreach (var runningTask in runningTasks)
            {
                FieldSelection fieldSelection = runningTask.task.Field;
                object fieldValue = await CompleteFieldValueAsync(
                    runningTask.resolverResult);

                TryCompleteValue(executionContext, runningTask.task.Source,
                    fieldSelection, fieldSelection.Field.Type,
                    runningTask.task.Path, fieldValue);
            }
        }

        private bool TryCompleteValue(
            ExecutionContext executionContext,
            ImmutableStack<object> source,
            FieldSelection fieldSelection,
            IType fieldType,
            Path path,
            object fieldValue)
        {
            object completedValue = fieldValue;

            if (fieldType.IsNonNullType())
            {
                IType innerType = fieldType.InnerType();
                if (!TryCompleteValue(
                    executionContext, source, fieldSelection,
                    innerType, path, completedValue))
                {
                    executionContext.Errors.Add(new FieldError(
                        "Cannot return null for non-nullable field.",
                        fieldSelection.Node));
                    executionContext.Result.AddValue(path, null);
                    return false;
                }
            }

            if (completedValue == null)
            {
                executionContext.Result.AddValue(path, null);
                return false;
            }

            if (fieldSelection.Field.Type.IsListType())
            {
                return TryCompleteListValue(executionContext, source, fieldSelection, fieldType, path, completedValue);
            }

            if (fieldSelection.Field.Type.IsScalarType()
                || fieldSelection.Field.Type.IsEnumType())
            {
                return TryCompleteScalarValue(executionContext, source, fieldSelection, fieldType, path, completedValue);
            }

            return TryCompleteObjectValue(executionContext, source, fieldSelection, fieldType, path, completedValue);
        }

        private bool TryCompleteListValue(
            ExecutionContext executionContext,
            ImmutableStack<object> source,
            FieldSelection fieldSelection,
            IType fieldType,
            Path path,
            object fieldValue)
        {
            IType elementType = fieldSelection.Field.Type.ElementType();
            bool isNonNullElement = elementType.IsNonNullType();
            int i = 0;

            foreach (object element in ((IEnumerable)fieldValue))
            {
                Path elementPath = path.Create(i++);
                bool hasValue = TryCompleteValue(
                    executionContext, source, fieldSelection,
                    elementType, elementPath, element);

                if (isNonNullElement && !hasValue)
                {
                    executionContext.Errors.Add(new FieldError(
                        "The list does not allow null elements",
                        fieldSelection.Node));
                    executionContext.Result.AddValue(path, null);
                    return false;
                }
            }

            return true;
        }

        private bool TryCompleteScalarValue(
            ExecutionContext executionContext,
            ImmutableStack<object> source,
            FieldSelection fieldSelection,
            IType fieldType,
            Path path,
            object fieldValue)
        {
            try
            {
                // TODO :   include enums
                executionContext.Result.AddValue(path, ((ScalarType)fieldType).Serialize(fieldValue));
                return true;
            }
            catch (ArgumentException ex)
            {
                executionContext.Errors.Add(new FieldError(ex.Message, fieldSelection.Node));
            }
            catch (Exception)
            {
                executionContext.Errors.Add(new FieldError("Undefined field serialization error.", fieldSelection.Node));
            }

            executionContext.Result.AddValue(path, null);
            return false;
        }

        private bool TryCompleteObjectValue(
            ExecutionContext executionContext,
            ImmutableStack<object> source,
            FieldSelection fieldSelection,
            IType fieldType,
            Path path,
            object fieldValue)
        {
            ObjectType objectType = ResolveObjectType(fieldType, fieldValue);

            IReadOnlyCollection<FieldSelection> fields = executionContext
                .FieldResolver.CollectFields(
                    objectType, fieldSelection.Node.SelectionSet,
                    executionContext.Errors.Add);

            foreach (FieldSelection field in fields)
            {
                executionContext.NextBatch.Add(new FieldResolverTask(
                    source.Push(fieldValue), objectType,
                    field, path.Create(field.ResponseName)));
            }

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
