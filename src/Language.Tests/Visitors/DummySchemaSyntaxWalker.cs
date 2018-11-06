using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class DummySchemaSyntaxWalker
        : SchemaSyntaxWalker
    {
        private HashSet<string> _visited = new HashSet<string>();

        public DummySchemaSyntaxWalker() { }

        public bool VisitedAllNodes => _visited.Count == 17;

        protected override void VisitSchemaDefinition(SchemaDefinitionNode node)
        {
            _visited.Add(nameof(VisitSchemaDefinition));
            base.VisitSchemaDefinition(node);
        }

        protected override void VisitSchemaExtension(SchemaExtensionNode node)
        {
            _visited.Add(nameof(VisitSchemaExtension));
            base.VisitSchemaExtension(node);
        }

        protected override void VisitOperationTypeDefinition(
            OperationTypeDefinitionNode node)
        {
            _visited.Add(nameof(VisitOperationTypeDefinition));
            base.VisitOperationTypeDefinition(node);
        }

        protected override void VisitDirectiveDefinition(
            DirectiveDefinitionNode node)
        {
            _visited.Add(nameof(VisitDirectiveDefinition));
            base.VisitDirectiveDefinition(node);
        }

        protected override void VisitScalarTypeDefinition(
            ScalarTypeDefinitionNode node)
        {
            _visited.Add(nameof(VisitScalarTypeDefinition));
            base.VisitScalarTypeDefinition(node);
        }

        protected override void VisitScalarTypeExtension(
            ScalarTypeExtensionNode node)
        {
            _visited.Add(nameof(VisitScalarTypeExtension));
            base.VisitScalarTypeExtension(node);
        }

        protected override void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node)
        {
            _visited.Add(nameof(VisitObjectTypeDefinition));
            base.VisitObjectTypeDefinition(node);
        }

        protected override void VisitObjectTypeExtension(
            ObjectTypeExtensionNode node)
        {
            _visited.Add(nameof(VisitObjectTypeExtension));
            base.VisitObjectTypeExtension(node);
        }

        protected override void VisitFieldDefinition(
            FieldDefinitionNode node)
        {
            _visited.Add(nameof(VisitFieldDefinition));
            base.VisitFieldDefinition(node);
        }

        protected override void VisitInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node)
        {
            _visited.Add(nameof(VisitInputObjectTypeDefinition));
            base.VisitInputObjectTypeDefinition(node);
        }

        protected override void VisitInputObjectTypeExtension(
            InputObjectTypeExtensionNode node)
        {
            _visited.Add(nameof(VisitInputObjectTypeExtension));
            base.VisitInputObjectTypeExtension(node);
        }

        protected override void VisitInterfaceTypeDefinition(
           InterfaceTypeDefinitionNode node)
        {
            _visited.Add(nameof(VisitInterfaceTypeDefinition));
            base.VisitInterfaceTypeDefinition(node);
        }

        protected override void VisitInterfaceTypeExtension(
            InterfaceTypeExtensionNode node)
        {
            _visited.Add(nameof(VisitInterfaceTypeExtension));
            base.VisitInterfaceTypeExtension(node);
        }

        protected override void VisitUnionTypeDefinition(
            UnionTypeDefinitionNode node)
        {
            _visited.Add(nameof(VisitUnionTypeDefinition));
            base.VisitUnionTypeDefinition(node);
        }

        protected override void VisitUnionTypeExtension(
            UnionTypeExtensionNode node)
        {
            _visited.Add(nameof(VisitUnionTypeExtension));
            base.VisitUnionTypeExtension(node);
        }

        protected override void VisitEnumTypeDefinition(
            EnumTypeDefinitionNode node)
        {
            _visited.Add(nameof(VisitEnumTypeDefinition));
            base.VisitEnumTypeDefinition(node);
        }

        protected override void VisitEnumTypeExtension(
            EnumTypeExtensionNode node)
        {
            _visited.Add(nameof(VisitEnumTypeExtension));
            base.VisitEnumTypeExtension(node);
        }
    }
}
