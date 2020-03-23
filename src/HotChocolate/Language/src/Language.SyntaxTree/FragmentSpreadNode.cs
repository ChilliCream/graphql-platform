using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class FragmentSpreadNode
        : NamedSyntaxNode
        , ISelectionNode
    {
        public FragmentSpreadNode(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives)
            : base(location, name, directives)
        { }

        public override NodeKind Kind { get; } = NodeKind.FragmentSpread;

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }
        }

        public override string ToString() => SyntaxPrinter.Print(this, true);

        public override string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public FragmentSpreadNode WithLocation(Location? location)
        {
            return new FragmentSpreadNode(location, Name, Directives);
        }

        public FragmentSpreadNode WithName(NameNode name)
        {
            return new FragmentSpreadNode(Location, name, Directives);
        }

        public FragmentSpreadNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new FragmentSpreadNode(Location, Name, directives);
        }
    }
}
