namespace HotChocolate.Language;

public interface IVisitationMap
{
    void ResolveChildren(
        ISyntaxNode node,
        IList<SyntaxNodeInfo> children);
}
