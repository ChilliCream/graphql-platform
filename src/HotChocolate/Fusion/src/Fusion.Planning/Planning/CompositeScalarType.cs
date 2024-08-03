using HotChocolate.Fusion.Planning.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class CompositeScalarType(
    string name,
    string? description,
    DirectiveCollection directives)
{
    public TypeKind Kind => TypeKind.Scalar;

    public string Name { get; } = name;

    public string? Description { get; } = description;

    public DirectiveCollection Directives { get; } = directives;
}
