using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class InputObjectTypeExtensionNode
        : InputObjectTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public InputObjectTypeExtensionNode(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<InputValueDefinitionNode> fields)
            : base(location, name, directives, fields)
        {
        }

        public override SyntaxKind Kind { get; } =
            SyntaxKind.InputObjectTypeExtension;

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            foreach (InputValueDefinitionNode field in Fields)
            {
                yield return field;
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

        public InputObjectTypeExtensionNode WithLocation(Location? location)
        {
            return new InputObjectTypeExtensionNode(
                location, Name, Directives, Fields);
        }

        public InputObjectTypeExtensionNode WithName(NameNode name)
        {
            return new InputObjectTypeExtensionNode(
                Location, name, Directives, Fields);
        }

        public InputObjectTypeExtensionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new InputObjectTypeExtensionNode(
                Location, Name, directives, Fields);
        }

        public InputObjectTypeExtensionNode WithFields(
            IReadOnlyList<InputValueDefinitionNode> fields)
        {
            return new InputObjectTypeExtensionNode(
                Location, Name, Directives, fields);
        }
    }
}
