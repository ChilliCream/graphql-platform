using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal static class QueryVisitorHelper
    {
        public static bool TryGetOperationType(
            this OperationDefinitionNode operation,
            ISchema schema,
            out ObjectType objectType)
        {
            return TryGetOperationType(
                operation.Operation,
                schema,
                out objectType);
        }

        private static bool TryGetOperationType(
            OperationType operation,
            ISchema schema,
            out ObjectType objectType)
        {
            switch (operation)
            {
                case OperationType.Query:
                    objectType = schema.QueryType;
                    break;

                case OperationType.Mutation:
                    objectType = schema.MutationType;
                    break;

                case Language.OperationType.Subscription:
                    objectType = schema.SubscriptionType;
                    break;

                default:
                    objectType = null;
                    break;
            }

            return objectType != null;
        }
    }
}
