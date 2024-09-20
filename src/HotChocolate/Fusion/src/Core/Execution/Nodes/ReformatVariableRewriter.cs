using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Transport.Http;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed class ReformatVariableRewriter : SyntaxRewriter<ReformatVariableRewriter>
{
    private static readonly ReformatVariableRewriter _instance = new();

    public static IValueNode Rewrite(IValueNode node)
    {
        if (_instance.Rewrite(node, _instance) is IValueNode rewritten)
        {
            return rewritten;
        }

        return NullValueNode.Default;
    }

    protected override IValueNode? RewriteCustomValue(IValueNode node, ReformatVariableRewriter context)
    {
        if (node is FileValueNode fileValueNode)
        {
            return new FileReferenceNode(
                fileValueNode.Value.OpenReadStream,
                fileValueNode.Value.Name);
        }

        return node;
    }
}
