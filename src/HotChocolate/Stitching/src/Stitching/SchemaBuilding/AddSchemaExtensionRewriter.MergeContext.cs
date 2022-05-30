using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.SchemaBuilding;

public partial class AddSchemaExtensionRewriter
{
    public class MergeContext : ISyntaxVisitorContext
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
