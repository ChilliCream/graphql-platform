namespace HotChocolate.Skimmed;

public sealed class Schema
{
    public TypeCollection Types { get; } = new();

    public DirectiveTypeCollection Directives { get; } = new();
}
