using HotChocolate.Types;

namespace HotChocolate.Fusion.SourceSchema.Types;

/// <summary>
/// The `@lookup` directive is used within a _source schema_ to specify output
/// fields that can be used by the _distributed GraphQL executor_ to resolve an
/// entity by a stable key.
/// </summary>
[DirectiveType("lookup", DirectiveLocation.FieldDefinition)]
public sealed class LookupDirective
{
    private LookupDirective() { }

    public static LookupDirective Instance { get; } = new();
}
