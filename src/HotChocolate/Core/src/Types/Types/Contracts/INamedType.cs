namespace HotChocolate.Types
{
    public interface INamedType
        : INullableType
        , IHasName
        , IHasDescription
        , IHasSyntaxNode
    {
        bool IsAssignableFrom(INamedType type);
    }
}
