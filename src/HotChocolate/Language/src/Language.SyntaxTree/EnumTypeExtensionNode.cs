using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class EnumTypeExtensionNode
        : EnumTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public EnumTypeExtensionNode(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<EnumValueDefinitionNode> values)
            : base(location, name, directives, values)
        {
        }

        public override SyntaxKind Kind { get; } = SyntaxKind.EnumTypeExtension;

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            foreach (EnumValueDefinitionNode value in Values)
            {
                yield return value;
            }
        }

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public override string ToString() => SyntaxPrinter.Print(this, true);

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <param name="indented">
        /// A value that indicates whether the GraphQL output should be formatted,
        /// which includes indenting nested GraphQL tokens, adding
        /// new lines, and adding white space between property names and values.
        /// </param>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public override string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public EnumTypeExtensionNode WithLocation(Location? location)
        {
            return new EnumTypeExtensionNode(
                location, Name,
                Directives, Values);
        }

        public EnumTypeExtensionNode WithName(NameNode name)
        {
            return new EnumTypeExtensionNode(
                Location, name,
                Directives, Values);
        }

        public EnumTypeExtensionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new EnumTypeExtensionNode(
                Location, Name,
                directives, Values);
        }

        public EnumTypeExtensionNode WithValues(
            IReadOnlyList<EnumValueDefinitionNode> values)
        {
            return new EnumTypeExtensionNode(
                Location, Name,
                Directives, values);
        }
    }
}
