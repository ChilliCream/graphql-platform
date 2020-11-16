using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class EnumValueDefinitionNode
        : NamedSyntaxNode
    {
        public EnumValueDefinitionNode(
            Location? location,
            NameNode name,
            StringValueNode? description,
            IReadOnlyList<DirectiveNode> directives)
            : base(location, name, directives)
        {
            Description = description;
        }

        public override SyntaxKind Kind { get; } = SyntaxKind.EnumValueDefinition;

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

        public EnumValueDefinitionNode WithLocation(Location? location)
        {
            return new EnumValueDefinitionNode(
                location, Name, Description, Directives);
        }

        public EnumValueDefinitionNode WithName(NameNode name)
        {
            return new EnumValueDefinitionNode(
                Location, name, Description, Directives);
        }

        public EnumValueDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new EnumValueDefinitionNode(
                Location, Name, description, Directives);
        }

        public EnumValueDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new EnumValueDefinitionNode(
                Location, Name, Description, directives);
        }
    }
}
