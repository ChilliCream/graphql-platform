using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Extensions;

namespace HotChocolate.Stitching.Types.Attempt1;

public static class SourceExtensions
{
    public static IEnumerable<DirectiveNode> PatchWithSchema(
        this IEnumerable<DirectiveNode> nodes,
        NameNode? source)
    {
        if (source is null)
        {
            foreach (DirectiveNode node in nodes)
            {
                yield return node;
            }

            yield break;
        }

        var sourceArgument = new ArgumentNode("schema", source.Value);

        foreach (DirectiveNode node in nodes)
        {
            yield return node.WithArguments(
                node.Arguments.AddOrReplace(sourceArgument,
                    argument => argument.IsEqualTo(sourceArgument)));
        }
    }
}
