using HotChocolate.Types;

namespace HotChocolate.Fusion.SourceSchema.Types;

[DirectiveType("lookup", DirectiveLocation.FieldDefinition)]
public sealed class LookupDirective
{
    private LookupDirective() { }

    public static LookupDirective Instance { get; } = new();
}
