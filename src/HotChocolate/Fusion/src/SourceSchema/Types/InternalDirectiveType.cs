using HotChocolate.Types;

namespace HotChocolate.Fusion.SourceSchema.Types;

/// <summary>
/// directive @internal on FIELD_DEFINITION
/// </summary>
[DirectiveType("internal", DirectiveLocation.FieldDefinition)]
public sealed class InternalDirective
{
    private InternalDirective() { }

    public static InternalDirective Instance { get; } = new();
}
