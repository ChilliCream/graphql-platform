using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    internal static class SchemaInfoExtensions
    {
        public static ObjectTypeDefinitionNode? GetRootType(
            this ISchemaInfo schema,
            OperationType operation)
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
                    throw new NotSupportedException();
            }
        }
    }
}
