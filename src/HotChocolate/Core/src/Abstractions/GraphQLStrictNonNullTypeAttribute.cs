namespace HotChocolate;

[AttributeUsage(
    AttributeTargets.Property
    | AttributeTargets.Method)]
public sealed class GraphQLStrictNonNullTypeAttribute : Attribute
{
}
