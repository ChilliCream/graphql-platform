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

        public override NodeKind Kind { get; } = NodeKind.SchemaExtension;

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

        public override string ToString() => SyntaxPrinter.Print(this, true);

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
