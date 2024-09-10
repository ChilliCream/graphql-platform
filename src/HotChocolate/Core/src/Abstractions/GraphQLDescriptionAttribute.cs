namespace HotChocolate;

[AttributeUsage(AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.Property
    | AttributeTargets.Method
    | AttributeTargets.Enum
    | AttributeTargets.Parameter
    | AttributeTargets.Field)]
public sealed class GraphQLDescriptionAttribute : Attribute
{
    public GraphQLDescriptionAttribute(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            throw new ArgumentNullException(nameof(description));
        }

        Description = description;
    }

    public string Description { get; }
}
