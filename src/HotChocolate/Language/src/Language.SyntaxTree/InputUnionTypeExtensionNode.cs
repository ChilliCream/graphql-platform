using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class InputUnionTypeExtensionNode
        : InputUnionTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public InputUnionTypeExtensionNode(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<NamedTypeNode> types)
            : base(location, name, directives, types)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.InputUnionTypeExtension;

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            foreach (NamedTypeNode type in Types)
            {
                yield return type;
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

        public InputUnionTypeExtensionNode WithLocation(Location? location)
        {
            return new InputUnionTypeExtensionNode(
                location, Name, Directives, Types);
        }

        public InputUnionTypeExtensionNode WithName(NameNode name)
        {
            return new InputUnionTypeExtensionNode(
                Location, name, Directives, Types);
        }

        public InputUnionTypeExtensionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new InputUnionTypeExtensionNode(
                Location, Name, directives, Types);
        }

        public InputUnionTypeExtensionNode WithTypes(
            IReadOnlyList<NamedTypeNode> types)
        {
            return new InputUnionTypeExtensionNode(
                Location, Name, Directives, types);
        }
    }
}
