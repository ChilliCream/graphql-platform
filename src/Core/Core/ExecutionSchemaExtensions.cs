using System;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    public static class ExecutionSchemaExtensions
    {
        public static IQueryExecuter MakeExecutable(this ISchema schema)
        {
            return QueryExecutionBuilder.BuildDefault(schema);
        }

        public static ObjectType GetOperationType(this ISchema schema, OperationType operation)
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
