using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class UnionTypeDefinitionNode
        : UnionTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public UnionTypeDefinitionNode(
            Location? location,
            NameNode name,
            StringValueNode? description,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<NamedTypeNode> types)
            : base(location, name, directives, types)
        {
            Description = description;
        }

        public override SyntaxKind Kind { get; } = SyntaxKind.UnionTypeDefinition;

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

        public UnionTypeDefinitionNode WithLocation(Location? location)
        {
            return new UnionTypeDefinitionNode(
                location, Name, Description,
                Directives, Types);
        }

        public UnionTypeDefinitionNode WithName(NameNode name)
        {
            return new UnionTypeDefinitionNode(
                Location, name, Description,
                Directives, Types);
        }

        public UnionTypeDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new UnionTypeDefinitionNode(
                Location, Name, description,
                Directives, Types);
        }

        public UnionTypeDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new UnionTypeDefinitionNode(
                Location, Name, Description,
                directives, Types);
        }

        public UnionTypeDefinitionNode WithTypes(
            IReadOnlyList<NamedTypeNode> types)
        {
            return new UnionTypeDefinitionNode(
                Location, Name, Description,
                Directives, types);
        }
    }
}
