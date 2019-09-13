using System;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    public static class ExecutionSchemaExtensions
    {
        public static IQueryExecutor MakeExecutable(
            this ISchema schema)
        {
            return QueryExecutionBuilder.BuildDefault(schema);
        }

        public static IQueryExecutor MakeExecutable(
            this ISchema schema,
            IQueryExecutionOptionsAccessor options)
        {
            return QueryExecutionBuilder.BuildDefault(schema, options);
        }

        public static IQueryExecutor MakeExecutable(
            this ISchema schema,
            Action<IQueryExecutionBuilder> configure)
        {
            QueryExecutionBuilder builder = QueryExecutionBuilder.New();
            configure(builder);
            return builder.Build(schema);
        }

        public static ObjectType GetOperationType(
            this ISchema schema,
            OperationType operation)
        {
            switch (operation)
            {
                case Language.OperationType.Query:
                    return schema.QueryType;
                case Language.OperationType.Mutation:
                    return schema.MutationType;
                case Language.OperationType.Subscription:
                    return schema.SubscriptionType;
                default:
                    throw new NotSupportedException();
            }
        }

        public static bool IsRootType(this ISchema schema, IType type)
        {
            if (type.IsObjectType())
            {
                return IsType(schema.QueryType, type)
                    || IsType(schema.MutationType, type)
                    || IsType(schema.SubscriptionType, type);
            }
            return false;
        }

        private static bool IsType(ObjectType left, IType right)
        {
            if (left == null)
            {
                return false;
            }

            return object.ReferenceEquals(left, right);
        }
    }
}
