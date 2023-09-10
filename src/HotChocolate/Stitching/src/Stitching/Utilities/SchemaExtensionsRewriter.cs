using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;
using static HotChocolate.Stitching.Utilities.SchemaExtensionsRewriter;

namespace HotChocolate.Stitching.Utilities;

public class SchemaExtensionsRewriter : SyntaxRewriter<Context>
{
    private readonly List<DirectiveNode> _directives = new();

    public IReadOnlyList<DirectiveNode> SchemaActions => _directives;

    protected override SchemaExtensionNode RewriteSchemaExtension(
        SchemaExtensionNode node,
        Context context)
    {
        var directives = new List<DirectiveNode>();

        foreach (var directive in node.Directives)
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
        Context context)
    {
        if (node.Name.Value.EqualsOrdinal(DirectiveNames.Delegate) &&
            !node.Arguments.Any(a => a.Name.Value.EqualsOrdinal(
                DirectiveFieldNames.Delegate_Schema)))
        {
            var arguments = node.Arguments.ToList();

            arguments.Add(new ArgumentNode(
                DirectiveFieldNames.Delegate_Schema,
                context.SchemaName));

            return node.WithArguments(arguments);
        }

        return node;
    }

    public sealed class Context : ISyntaxVisitorContext
    {
        public Context(string schemaName)
        {
            SchemaName = schemaName;
        }

        public string SchemaName { get; }
    }
}
