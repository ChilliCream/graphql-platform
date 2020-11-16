namespace HotChocolate.Types
{
    public interface INamedType
        : INullableType
        , IHasName
        , IHasDescription
        , IHasSyntaxNode
        , IHasReadOnlyContextData
    {
        bool IsAssignableFrom(INamedType type);
    }
}
