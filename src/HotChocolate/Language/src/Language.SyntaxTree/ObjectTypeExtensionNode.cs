using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class ObjectTypeExtensionNode
        : ObjectTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public ObjectTypeExtensionNode(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<NamedTypeNode> interfaces,
            IReadOnlyList<FieldDefinitionNode> fields)
            : base(location, name, directives, interfaces, fields)
        { }

        public override SyntaxKind Kind { get; } = SyntaxKind.ObjectTypeExtension;

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;

            foreach (NamedTypeNode interfaceName in Interfaces)
            {
                yield return interfaceName;
            }

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            foreach (FieldDefinitionNode field in Fields)
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

        public ObjectTypeExtensionNode WithLocation(Location? location)
        {
            return new ObjectTypeExtensionNode(
                location, Name, Directives,
                Interfaces, Fields);
        }

        public ObjectTypeExtensionNode WithName(NameNode name)
        {
            return new ObjectTypeExtensionNode(
                Location, name, Directives,
                Interfaces, Fields);
        }

        public ObjectTypeExtensionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new ObjectTypeExtensionNode(
                Location, Name, directives,
                Interfaces, Fields);
        }

        public ObjectTypeExtensionNode WithInterfaces(
            IReadOnlyList<NamedTypeNode> interfaces)
        {
            return new ObjectTypeExtensionNode(
                Location, Name, Directives,
                interfaces, Fields);
        }

        public ObjectTypeExtensionNode WithFields(
            IReadOnlyList<FieldDefinitionNode> fields)
        {
            return new ObjectTypeExtensionNode(
                Location, Name, Directives,
                Interfaces, fields);
        }
    }
}
