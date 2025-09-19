namespace HotChocolate;

[AttributeUsage(AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.Property
    | AttributeTargets.Method
    | AttributeTargets.Parameter
    | AttributeTargets.Enum
    | AttributeTargets.Field)]
public sealed class GraphQLNameAttribute : Attribute
{
    public GraphQLNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Name = name;
    }

    public string Name { get; }
}
