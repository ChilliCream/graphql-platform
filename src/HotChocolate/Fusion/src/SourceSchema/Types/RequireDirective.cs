using HotChocolate.Types;

namespace HotChocolate.Fusion.SourceSchema.Types;

[DirectiveType("require", DirectiveLocation.ArgumentDefinition)]
public sealed class RequireDirective
{
    public RequireDirective(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            throw new ArgumentException(
                "Value cannot be null or empty.",
                nameof(field));
        }

        Field = field;
    }

    [GraphQLName("field")]
    public string Field { get; }
}
