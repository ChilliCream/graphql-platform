using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Extensions;

internal static class SchemaDefinitionExtensions
{
    public static bool IsRootOperationType(this SchemaDefinition schema, ObjectTypeDefinition type)
    {
        return
            schema.QueryType == type
            || schema.MutationType == type
            || schema.SubscriptionType == type;
    }
}
