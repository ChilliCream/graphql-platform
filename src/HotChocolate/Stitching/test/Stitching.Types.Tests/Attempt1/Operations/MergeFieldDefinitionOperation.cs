using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Helpers;
using HotChocolate.Stitching.Types.Extensions;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class MergeFieldDefinitionOperation : ISchemaNodeOperation<FieldDefinition>
{
    public void Apply(FieldDefinition source, FieldDefinition target, MergeOperationContext context)
    {
        source.MergeDirectivesInto(target);

        var sourceName = source.Database.Name;
        if (sourceName is null)
        {
            return;
        }

        var sourceDirectiveNode = new DirectiveNode("_hc_source",
            new ArgumentNode("schema", sourceName));

        IReadOnlyList<DirectiveNode> updatedDirectives = target
            .Definition
            .Directives
            .AddOrReplace(sourceDirectiveNode,
                node => sourceDirectiveNode.IsEqualTo(node));

        FieldDefinitionNode fieldDefinitionNode = target.Definition
            .WithDirectives(updatedDirectives);

        target.RewriteDefinition(fieldDefinitionNode);
    }
}
