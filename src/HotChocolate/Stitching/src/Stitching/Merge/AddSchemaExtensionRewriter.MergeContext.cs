using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    public partial class AddSchemaExtensionRewriter
    {
        public class MergeContext
        {
            public MergeContext(DocumentNode schema, DocumentNode extensions)
            {
                Extensions = extensions.Definitions
                    .OfType<ITypeExtensionNode>()
                    .ToDictionary(t => t.Name.Value);

                Directives = schema.Definitions
                    .OfType<DirectiveDefinitionNode>()
                    .ToDictionary(t => t.Name.Value);
            }

            public IDictionary<string, ITypeExtensionNode> Extensions { get; }

            public IDictionary<string, DirectiveDefinitionNode> Directives { get; }
        }
    }
}
