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
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        Name = name;
    }

    public string Name { get; }
}
