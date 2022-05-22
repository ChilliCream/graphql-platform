using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language.Contracts;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language.Rewriters;

public class SchemaSyntaxRewriterWithNavigationTests
{
    [Fact]
    public void DirectiveNavigation()
    {
        const string document = @"
type Query {
  test: String @test_directive
}
";
        DocumentNode documentNode = Utf8GraphQLParser.Parse(document);

        var captures = new List<IReadOnlyList<ISyntaxNode>>();
        var rewriter = new TestDirective(captures);
        rewriter.Rewrite(documentNode, new Context());

        captures
            .Single()
            .GetNames()
            .MatchSnapshot();
    }

    [Fact]
    public void InterfaceNavigation()
    {
        const string document = @"
type Query implements TestInterface {
  test: String
}
";

        DocumentNode documentNode = Utf8GraphQLParser.Parse(document);

        var captures = new List<IReadOnlyList<ISyntaxNode>>();
        var rewriter = new TestInterface(captures);
        rewriter.Rewrite(documentNode, new Context());

        captures
            .Single()
            .GetNames()
            .MatchSnapshot();
    }

    private class TestSchemaSyntaxRewriterWithNavigationBase
        : SchemaSyntaxRewriterWithNavigation<Context>
    {
        private readonly List<IReadOnlyList<ISyntaxNode>> _captures;

        public TestSchemaSyntaxRewriterWithNavigationBase(List<IReadOnlyList<ISyntaxNode>> captures)
        {
            _captures = captures;
        }

        protected void Capture(ISyntaxNode node, Context context)
        {
            var capture = new List<ISyntaxNode> { node };

            capture.AddRange(context.Navigator
                .GetAncestors<ISyntaxNode>());

            // Reverse the collection so the ancestor tree makes more sense in the snapshots.
            capture.Reverse();

            _captures.Add(capture);
        }
    }

    private class TestDirective
        : TestSchemaSyntaxRewriterWithNavigationBase
    {
        public TestDirective(List<IReadOnlyList<ISyntaxNode>> captures)
            : base(captures)
        {
        }

        protected override DirectiveNode RewriteDirective(DirectiveNode node, Context context)
        {
            Capture(node, context);
            return base.RewriteDirective(node, context);
        }
    }

    private class TestInterface
        : TestSchemaSyntaxRewriterWithNavigationBase
    {
        public TestInterface(List<IReadOnlyList<ISyntaxNode>> captures)
            : base(captures)
        {
        }

        protected override NamedTypeNode RewriteNamedType(NamedTypeNode node, Context context)
        {
            if (context.Navigator.Parent is ObjectTypeDefinitionNode)
            {
                Capture(node, context);
            }

            return base.RewriteNamedType(node, context);
        }
    }

    private class Context : INavigatorContext
    {
        public Context()
        {
            Navigator = new DefaultSyntaxNavigator();
        }

        public ISyntaxNavigator Navigator { get; }
    }
}
