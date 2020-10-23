using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Utilities
{
    public class SchemaExtensionsRewriter : SchemaSyntaxRewriter<string>
    {
        private readonly List<DirectiveNode> _directives = new List<DirectiveNode>();

        public IReadOnlyList<DirectiveNode> SchemaActions => _directives;

        protected override SchemaExtensionNode RewriteSchemaExtension(
            SchemaExtensionNode node,
            string context)
        {
            var directives = new List<DirectiveNode>();

            foreach (DirectiveNode directive in node.Directives)
            {
                switch (directive.Name.Value)
                {
                    case DirectiveNames.RemoveType:
                    case DirectiveNames.RenameField:
                    case DirectiveNames.RenameType:
                    case DirectiveNames.RemoveRootTypes:
                        _directives.Add(directive);
                        break;

                    default:
                        directives.Add(directive);
                        break;
                }
            }

            return node.WithDirectives(directives);
        }

        protected override DirectiveNode RewriteDirective(
            DirectiveNode node,
            string context)
        {
            if (node.Name.Value.EqualsOrdinal(DirectiveNames.Delegate) &&
                !node.Arguments.Any(a => a.Name.Value.EqualsOrdinal(
                    DirectiveFieldNames.Delegate_Schema)))
            {
                var arguments = node.Arguments.ToList();

                arguments.Add(new ArgumentNode(
                    DirectiveFieldNames.Delegate_Schema,
                    context));

                return node.WithArguments(arguments);
            }

            return node;
        }
    }
}
