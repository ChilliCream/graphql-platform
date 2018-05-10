using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class ExecutionContext
    {
        public Schema Schema { get; }
        public FragmentCollection Fragments { get; }
        public object RootValue { get; }
        public object UserContext { get; }
        public OperationDefinitionNode Operation { get; }
        public VariableCollection Variables { get; }
        public List<IQueryError> Errors { get; }
        public QueryResultBuilder Result { get; }
        public List<FieldResolverTask> NextBatch { get; }

        // contextValue: mixed,
        // fieldResolver: GraphQLFieldResolver<any, any>,
    }

    public class Exp
    {

        public void ExecuteFields(
            ExecutionContext executionContext,
            ObjectType objectType,
            ImmutableQueue<object> source,
            FieldNode[] fields, // field map
            string path)
        {

        }

        public object ResolveField(
            ExecutionContext executionContext,
            ObjectType objectType,
            ImmutableQueue<object> source,
            FieldNode[] fields,
            string path)
        {
            return null;
        }


        public object CompleteValue(
            ExecutionContext executionContext,
            ImmutableQueue<object> source,
            FieldSelection fieldSelection,
            IType fieldType,
            Path path,
            object fieldValue)
        {
            object completedValue = fieldValue;

            if (fieldType.IsNonNullType())
            {
                IType innerType = fieldType.InnerType();
                completedValue = CompleteValue(
                    executionContext, source, fieldSelection,
                    innerType, path, completedValue);

                if (completedValue == null)
                {
                    executionContext.Errors.Add(new FieldError(
                        "Cannot return null for non-nullable field.",
                        fieldSelection.Node));
                    return null;
                }
            }

            if (completedValue == null)
            {
                return null;
            }

            if (fieldSelection.Field.Type.IsListType())
            {
                return CompleteListValue(executionContext, source, fieldSelection, fieldType, path, completedValue);
            }

            if (fieldSelection.Field.Type.IsScalarType()
                || fieldSelection.Field.Type.IsEnumType())
            {

            }

            // must be an object than
        }

        private object CompleteListValue(
            ExecutionContext executionContext,
            ImmutableQueue<object> source,
            FieldSelection fieldSelection,
            IType fieldType,
            Path path,
            object fieldValue)
        {
            IType elementType = fieldSelection.Field.Type.ElementType();
            bool isNonNullElement = elementType.IsNonNullType();
            int i = 0;

            List<object> list = new List<object>();
            foreach (object element in ((IEnumerable)fieldValue))
            {
                Path elementPath = path.Create(i++);
                object elementValue = CompleteValue(
                    executionContext, source.Enqueue(element), fieldSelection,
                    elementType, elementPath, element);

                if (isNonNullElement && element == null)
                {
                    executionContext.Errors.Add(new FieldError(
                        "The list does not allow null elements",
                        fieldSelection.Node));
                    return null;
                }

                list.Add(elementValue);
            }

            return list;
        }

        private object CompleteScalarValue(
            ExecutionContext executionContext,
            ImmutableQueue<object> source,
            FieldSelection fieldSelection,
            IType fieldType,
            Path path,
            object fieldValue)
        {
            try
            {
                // TODO :   include enums
                return ((ScalarType)fieldType).Serialize(fieldValue);
            }
            catch (ArgumentException ex)
            {
                executionContext.Errors.Add(new FieldError(ex.Message, fieldSelection.Node));
            }
            catch (Exception ex)
            {
                executionContext.Errors.Add(new FieldError("Undefined field serialization error.", fieldSelection.Node));
            }
            return null;
        }

        private async Task<object> CompleteFieldValue(object resolverResult)
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

    internal readonly struct FieldResolverTask
    {
        public Path Path { get; }
        public ObjectType ObjectType { get; }
        public FieldSelection Field { get; }
        public ImmutableQueue<object> Source { get; }
    }

    internal readonly struct Path
    {
        public Path Create(int index)
        {

        }

        public Path Create(string name)
        {

        }
    }

    internal class QueryResultBuilder
    {
        public void AddValue(in Path path, object value)
        {

        }
    }
}
