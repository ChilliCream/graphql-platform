using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    public static class SchemaExtensions
    {
        /// <summary>
        /// Get the root operation object type.
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <param name="operation">The operation type.</param>
        /// <returns>
        /// Returns the root operation object type.
        /// </returns>
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