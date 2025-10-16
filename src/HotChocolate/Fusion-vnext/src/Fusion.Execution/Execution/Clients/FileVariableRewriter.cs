using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Transport.Http;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Clients;

internal sealed class FileVariableRewriter : SyntaxRewriter<FileVariableRewriter>
{
    private static readonly FileVariableRewriter s_instance = new();

    public static IValueNode Rewrite(IValueNode node)
    {
        if (s_instance.Rewrite(node, s_instance) is IValueNode rewritten)
        {
            return rewritten;
        }

        return NullValueNode.Default;
    }

    protected override IValueNode? RewriteCustomValue(IValueNode node, FileVariableRewriter context)
    {
        if (node is FileValueNode fileValueNode)
        {
            return new FileReferenceNode(
                fileValueNode.Value.OpenReadStream,
                fileValueNode.Value.Name,
                fileValueNode.Value.ContentType);
        }

        return node;
    }
}
