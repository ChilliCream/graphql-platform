using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Extensions;

namespace HotChocolate.Stitching.Types.Attempt1;

public static class SourceExtensions
{
    public static IEnumerable<DirectiveNode> PatchWithSchema(
        this IEnumerable<DirectiveNode> nodes,
        ISchemaDatabase database)
    {
        if (database.Name is null)
        {
            foreach (DirectiveNode node in nodes)
            {
                yield return node;
            }

            yield break;
        }

        var sourceArgument = new ArgumentNode("schema", database.Name);

        foreach (DirectiveNode node in nodes)
        {
            yield return node.WithArguments(
                node.Arguments.AddOrReplace(sourceArgument,
                    argument => argument.IsEqualTo(sourceArgument)));
        }
    }
}
