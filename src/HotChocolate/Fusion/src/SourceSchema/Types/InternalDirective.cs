using HotChocolate.Types;

namespace HotChocolate.Fusion.SourceSchema.Types;

[DirectiveType("internal", DirectiveLocation.FieldDefinition)]
public sealed class InternalDirective
{
    private InternalDirective() { }

    public static InternalDirective Instance { get; } = new();
}
