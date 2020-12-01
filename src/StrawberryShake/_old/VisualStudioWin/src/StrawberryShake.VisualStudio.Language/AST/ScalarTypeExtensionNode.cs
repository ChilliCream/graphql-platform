using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public sealed class ScalarTypeExtensionNode
        : ScalarTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public ScalarTypeExtensionNode(
            Location location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives)
            : base(location, name, directives)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.ScalarTypeExtension;

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }
        }

        public ScalarTypeExtensionNode WithLocation(Location location)
        {
            return new ScalarTypeExtensionNode(
                location, Name, Directives);
        }

        public ScalarTypeExtensionNode WithName(NameNode name)
        {
            return new ScalarTypeExtensionNode(
                Location, name, Directives);
        }

        public ScalarTypeExtensionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new ScalarTypeExtensionNode(
                Location, Name, directives);
        }
    }
}
