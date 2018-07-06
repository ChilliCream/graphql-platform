using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal static class SchemaExecutionExtensions
    {
        public static ObjectType GetOperationType(
            this Schema schema, OperationType operation)
        {
            switch (operation)
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
    }
}
