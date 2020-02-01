using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class DummySchemaSyntaxWalker
        : SchemaSyntaxWalker<object>
    {
        private HashSet<string> _visited = new HashSet<string>();

        public DummySchemaSyntaxWalker() { }

        public bool VisitedAllNodes => _visited.Count == 17;

        protected override void VisitSchemaDefinition(
            SchemaDefinitionNode node,
            object context)
        {
            _visited.Add(nameof(VisitSchemaDefinition));
            base.VisitSchemaDefinition(node, context);
        }

        protected override void VisitSchemaExtension(
            SchemaExtensionNode node,
            object context)
        {
            _visited.Add(nameof(VisitSchemaExtension));
            base.VisitSchemaExtension(node, context);
        }

        protected override void VisitOperationTypeDefinition(
            OperationTypeDefinitionNode node,
            object context)
        {
            _visited.Add(nameof(VisitOperationTypeDefinition));
            base.VisitOperationTypeDefinition(node, context);
        }

        protected override void VisitDirectiveDefinition(
            DirectiveDefinitionNode node,
            object context)
        {
            _visited.Add(nameof(VisitDirectiveDefinition));
            base.VisitDirectiveDefinition(node, context);
        }

        protected override void VisitScalarTypeDefinition(
            ScalarTypeDefinitionNode node,
            object context)
        {
            _visited.Add(nameof(VisitScalarTypeDefinition));
            base.VisitScalarTypeDefinition(node, context);
        }

        protected override void VisitScalarTypeExtension(
            ScalarTypeExtensionNode node,
            object context)
        {
            _visited.Add(nameof(VisitScalarTypeExtension));
            base.VisitScalarTypeExtension(node, context);
        }

        protected override void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            object context)
        {
            _visited.Add(nameof(VisitObjectTypeDefinition));
            base.VisitObjectTypeDefinition(node, context);
        }

        protected override void VisitObjectTypeExtension(
            ObjectTypeExtensionNode node,
            object context)
        {
            _visited.Add(nameof(VisitObjectTypeExtension));
            base.VisitObjectTypeExtension(node, context);
        }

        protected override void VisitFieldDefinition(
            FieldDefinitionNode node,
            object context)
        {
            _visited.Add(nameof(VisitFieldDefinition));
            base.VisitFieldDefinition(node, context);
        }

        protected override void VisitInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node,
            object context)
        {
            _visited.Add(nameof(VisitInputObjectTypeDefinition));
            base.VisitInputObjectTypeDefinition(node, context);
        }

        protected override void VisitInputObjectTypeExtension(
            InputObjectTypeExtensionNode node,
            object context)
        {
            _visited.Add(nameof(VisitInputObjectTypeExtension));
            base.VisitInputObjectTypeExtension(node, context);
        }

        protected override void VisitInterfaceTypeDefinition(
           InterfaceTypeDefinitionNode node,
            object context)
        {
            _visited.Add(nameof(VisitInterfaceTypeDefinition));
            base.VisitInterfaceTypeDefinition(node, context);
        }

        protected override void VisitInterfaceTypeExtension(
            InterfaceTypeExtensionNode node,
            object context)
        {
            _visited.Add(nameof(VisitInterfaceTypeExtension));
            base.VisitInterfaceTypeExtension(node, context);
        }

        protected override void VisitUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            object context)
        {
            _visited.Add(nameof(VisitUnionTypeDefinition));
            base.VisitUnionTypeDefinition(node, context);
        }

        protected override void VisitUnionTypeExtension(
            UnionTypeExtensionNode node,
            object context)
        {
            _visited.Add(nameof(VisitUnionTypeExtension));
            base.VisitUnionTypeExtension(node, context);
        }

        protected override void VisitEnumTypeDefinition(
            EnumTypeDefinitionNode node,
            object context)
        {
            _visited.Add(nameof(VisitEnumTypeDefinition));
            base.VisitEnumTypeDefinition(node, context);
        }

        protected override void VisitEnumTypeExtension(
            EnumTypeExtensionNode node,
            object context)
        {
            _visited.Add(nameof(VisitEnumTypeExtension));
            base.VisitEnumTypeExtension(node, context);
        }
    }
}
