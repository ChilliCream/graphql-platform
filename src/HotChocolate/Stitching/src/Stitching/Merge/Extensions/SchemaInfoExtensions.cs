using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge;

internal static class SchemaInfoExtensions
{
    public static ObjectTypeDefinitionNode? GetRootType(
        this ISchemaInfo schema,
        OperationType operation)
        => operation switch
        {
            OperationType.Query => schema.QueryType,
            OperationType.Mutation => schema.MutationType,
            OperationType.Subscription => schema.SubscriptionType,
            _ => throw new NotSupportedException(),
        };
}
