namespace HotChocolate.Language
{
    public interface IVisitationMap
    {
        void ResolveChildren(
            ISyntaxNode node,
            IStack<SyntaxNodeInfo> children);
    }
}
