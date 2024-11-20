using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public interface IOperationNode
{
    ISyntaxNode ToSyntaxNode();
}
