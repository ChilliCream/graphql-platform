using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class SchemaExtensionNode
        : SchemaDefinitionNodeBase
        , ITypeSystemExtensionNode
    {
        public SchemaExtensionNode(
            Location? location,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
            : base(location, directives, operationTypes)
        {
        }

        public override SyntaxKind Kind { get; } = SyntaxKind.SchemaExtension;

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            foreach (OperationTypeDefinitionNode operationType in OperationTypes)
            {
                yield return operationType;
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
        public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public SchemaExtensionNode WithLocation(Location? location)
        {
            return new SchemaExtensionNode(
                Location, Directives, OperationTypes);
        }

        public SchemaExtensionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new SchemaExtensionNode(
                Location, directives, OperationTypes);
        }

        public SchemaExtensionNode WithOperationTypes(
            IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
        {
            return new SchemaExtensionNode(
                Location, Directives, operationTypes);
        }
    }
}
