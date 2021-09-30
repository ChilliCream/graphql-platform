using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.SchemaBuilding
{
    public class Class1
    {
    }

    public class NodeIdentifier
    {

    }

    public class S : SyntaxVisitor<ISchemaRewriterContext>
    {
        private static HashSet<SyntaxKind> _types = new()
        {
            SyntaxKind.EnumTypeDefinition,
            SyntaxKind.EnumTypeExtension,
            SyntaxKind.ObjectTypeDefinition,
            SyntaxKind.ObjectTypeExtension,
            SyntaxKind.InterfaceTypeDefinition,
            SyntaxKind.InterfaceTypeExtension,
            SyntaxKind.UnionTypeDefinition,
            SyntaxKind.UnionTypeExtension,
            SyntaxKind.EnumTypeDefinition,
            SyntaxKind.EnumTypeExtension,
            SyntaxKind.EnumTypeDefinition,
            SyntaxKind.EnumTypeExtension,
        }


        protected override ISyntaxVisitorAction Enter(
            ISyntaxNode node,
            ISchemaRewriterContext context)
        {


            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Leave(
            ISyntaxNode node,
            ISchemaRewriterContext context)
        {


            return base.Leave(node, context);
        }
    }

    public interface ISchemaRewriter
    {
        void Inspect(DirectiveNode directive, ISchemaRewriterContext context);

        ISyntaxNode Rewrite(ISyntaxNode node, ISchemaRewriterContext context);
    }


    public interface ISchemaRewriterContext : ISyntaxVisitorContext
    {
        SchemaConfiguration Configuration { get; }

        List<ISyntaxNode> Path { get; }


    }

    public class SchemaConfiguration
    {
        public HashSet<OperationType> IncludedOperations { get; } = new();

        public HashSet<string> IgnoreDirectiveDeclarations { get; } = new();
    }
}
