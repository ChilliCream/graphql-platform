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
