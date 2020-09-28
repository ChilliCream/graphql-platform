using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class InputObjectTypeDefinitionNode
        : InputObjectTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public InputObjectTypeDefinitionNode(
            Location? location,
            NameNode name,
            StringValueNode? description,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<InputValueDefinitionNode> fields)
            : base(location, name, directives, fields)
        {
            Description = description;
        }

        public override SyntaxKind Kind { get; } =
            SyntaxKind.InputObjectTypeDefinition;

        public StringValueNode? Description { get; }

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            if (Description is { })
            {
                yield return Description;
            }

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

        public InputObjectTypeDefinitionNode WithLocation(Location? location)
        {
            return new InputObjectTypeDefinitionNode(
                location, Name, Description,
                Directives, Fields);
        }

        public InputObjectTypeDefinitionNode WithName(NameNode name)
        {
            return new InputObjectTypeDefinitionNode(
                Location, name, Description,
                Directives, Fields);
        }

        public InputObjectTypeDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new InputObjectTypeDefinitionNode(
                Location, Name, description,
                Directives, Fields);
        }

        public InputObjectTypeDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new InputObjectTypeDefinitionNode(
                Location, Name, Description,
                directives, Fields);
        }

        public InputObjectTypeDefinitionNode WithFields(
            IReadOnlyList<InputValueDefinitionNode> fields)
        {
            return new InputObjectTypeDefinitionNode(
                Location, Name, Description,
                Directives, fields);
        }
    }
}
