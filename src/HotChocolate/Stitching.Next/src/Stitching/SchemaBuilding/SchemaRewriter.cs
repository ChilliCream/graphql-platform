using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.SchemaBuilding
{
    public class SchemaRewriter : SchemaSyntaxRewriter<ISchemaRewriterContext>
    {
        private static readonly HashSet<SyntaxKind> _visit = new()
        {
            SyntaxKind.ObjectTypeDefinition,
            SyntaxKind.ObjectTypeExtension,
            SyntaxKind.InterfaceTypeDefinition,
            SyntaxKind.InterfaceTypeExtension,
            SyntaxKind.UnionTypeDefinition,
            SyntaxKind.UnionTypeExtension,
            SyntaxKind.ScalarTypeDefinition,
            SyntaxKind.ScalarTypeExtension,
            SyntaxKind.InputObjectTypeDefinition,
            SyntaxKind.InputObjectTypeExtension,
            SyntaxKind.SchemaDefinition,
            SyntaxKind.SchemaExtension
        };

        protected override ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteObjectTypeDefinition(current, context);
            context.Path.Pop();

            return current;
        }

        protected override ObjectTypeExtensionNode RewriteObjectTypeExtension(
            ObjectTypeExtensionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteObjectTypeExtension(current, context);
            context.Path.Pop();

            return current;
        }

        protected override InterfaceTypeDefinitionNode RewriteInterfaceTypeDefinition(
            InterfaceTypeDefinitionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteInterfaceTypeDefinition(current, context);
            context.Path.Pop();

            return current;
        }

        protected override InterfaceTypeExtensionNode RewriteInterfaceTypeExtension(
            InterfaceTypeExtensionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteInterfaceTypeExtension(current, context);
            context.Path.Pop();

            return current;
        }

        protected override UnionTypeDefinitionNode RewriteUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteUnionTypeDefinition(current, context);
            context.Path.Pop();

            return current;
        }

        protected override UnionTypeExtensionNode RewriteUnionTypeExtension(
            UnionTypeExtensionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteUnionTypeExtension(current, context);
            context.Path.Pop();

            return current;
        }

        protected override InputObjectTypeDefinitionNode RewriteInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteInputObjectTypeDefinition(current, context);
            context.Path.Pop();

            return current;
        }

        protected override InputObjectTypeExtensionNode RewriteInputObjectTypeExtension(
            InputObjectTypeExtensionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteInputObjectTypeExtension(current, context);
            context.Path.Pop();

            return current;
        }

        protected override EnumTypeDefinitionNode RewriteEnumTypeDefinition(
            EnumTypeDefinitionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteEnumTypeDefinition(current, context);
            context.Path.Pop();

            return current;
        }

        protected override EnumTypeExtensionNode RewriteEnumTypeExtension(
            EnumTypeExtensionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteEnumTypeExtension(current, context);
            context.Path.Pop();

            return current;
        }

        protected override ScalarTypeDefinitionNode RewriteScalarTypeDefinition(
            ScalarTypeDefinitionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteScalarTypeDefinition(current, context);
            context.Path.Pop();

            return current;
        }

        protected override ScalarTypeExtensionNode RewriteScalarTypeExtension(
            ScalarTypeExtensionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteScalarTypeExtension(current, context);
            context.Path.Pop();

            return current;
        }

        protected override SchemaDefinitionNode RewriteSchemaDefinition(
            SchemaDefinitionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteSchemaDefinition(current, context);
            context.Path.Pop();

            return current;
        }

        protected override SchemaExtensionNode RewriteSchemaExtension(
            SchemaExtensionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteSchemaExtension(current, context);
            context.Path.Pop();

            return current;
        }
        
        protected override FieldDefinitionNode RewriteFieldDefinition(
            FieldDefinitionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteFieldDefinition(current, context);
            context.Path.Pop();

            return current;
        }

        protected override InputValueDefinitionNode RewriteInputValueDefinition(
            InputValueDefinitionNode node,
            ISchemaRewriterContext context)
        {
            var current = node;

            current = ApplyRewriter(current, context);

            context.Path.Push(current);
            current = base.RewriteInputValueDefinition(current, context);
            context.Path.Pop();

            return current;
        }
        
        private TSyntaxNode ApplyRewriter<TSyntaxNode>(
            TSyntaxNode node,
            ISchemaRewriterContext context)
            where TSyntaxNode : ISyntaxNode
        {
            TSyntaxNode current = node;

            foreach (ISchemaRewriter rewriter in context.Rewriters)
            {
                current = (TSyntaxNode)rewriter.Rewrite(current, context);
            }

            return current;
        }
    }
}
