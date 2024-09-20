namespace HotChocolate;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
public sealed class GraphQLIgnoreAttribute : Attribute
{
}
