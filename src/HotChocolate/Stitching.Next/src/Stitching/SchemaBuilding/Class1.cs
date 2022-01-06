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

    public interface ISchemaRewriter
    {
        void Inspect(DirectiveNode directive, ISchemaRewriterContext context);

        ISyntaxNode Rewrite(ISyntaxNode node, ISchemaRewriterContext context);
    }

    public interface ISchemaRewriterContext : ISyntaxVisitorContext
    {
        ISyntaxNode? Node { get; set; }

        SchemaConfiguration Configuration { get; }

        List<ISyntaxNode> Path { get; }

        List<ISchemaRewriter> Rewriters { get; }
    }

    public class SchemaRewriterContext : ISchemaRewriterContext
    {
        public ISyntaxNode? Node { get; set; }

        public SchemaConfiguration Configuration { get; }

        public List<ISyntaxNode> Path { get; } = new();

        public List<ISchemaRewriter> Rewriters { get; } = new();
    }

    public class SchemaConfiguration
    {
        public HashSet<OperationType> IncludedOperations { get; } = new();

        public HashSet<string> IgnoreDirectiveDeclarations { get; } = new();
    }
}
