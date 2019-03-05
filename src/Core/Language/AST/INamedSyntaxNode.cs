namespace HotChocolate.Language
{
    public interface INamedSyntaxNode
        : ISyntaxNode
        , IHasName
        , IHasDirectives
    {
    }
}
