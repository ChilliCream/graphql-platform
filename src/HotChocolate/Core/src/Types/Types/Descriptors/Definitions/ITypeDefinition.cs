namespace HotChocolate.Types.Descriptors.Definitions
{
    public interface ITypeDefinition
        : IHasSyntaxNode
        , IHasRuntimeType
        , IHasDirectiveDefinition
        , IHasExtendsType
    {
    }
}
