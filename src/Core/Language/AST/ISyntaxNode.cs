namespace HotChocolate.Language
{
    public interface ISyntaxNode
    {
        NodeKind Kind { get; }

        Location? Location { get; }
    }
}
