namespace StrawberryShake.VisualStudio.Language
{
    /// <summary>
    /// Represents named syntax nodes.
    /// </summary>
    public interface INamedSyntaxNode
        : ISyntaxNode
        , IHasName
        , IHasDirectives
    {
    }
}
