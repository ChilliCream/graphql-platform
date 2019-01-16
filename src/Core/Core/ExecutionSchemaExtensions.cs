using System;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    public static class ExecutionSchemaExtensions
    {
        public static IQueryExecuter MakeExecutable(
            this ISchema schema)
        {
            return QueryExecutionBuilder.BuildDefault(schema);
        }

        public static IQueryExecuter MakeExecutable(
            this ISchema schema,
            IQueryExecutionOptionsAccessor options)
        {
            return QueryExecutionBuilder.BuildDefault(schema, options);
        }

        public static IQueryExecuter MakeExecutable(
            this ISchema schema,
            Func<IQueryExecutionBuilder, IQueryExecutionBuilder> configure)
        {
            IQueryExecutionBuilder builder = configure(
                QueryExecutionBuilder.New());

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
    }
}
