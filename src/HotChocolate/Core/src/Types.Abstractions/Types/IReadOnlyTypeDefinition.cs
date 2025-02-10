namespace HotChocolate.Types;

public interface IReadOnlyTypeDefinition : ISyntaxNodeProvider
{
    TypeKind Kind { get; }

    bool IsAssignableFrom(IReadOnlyTypeDefinition type);
}
