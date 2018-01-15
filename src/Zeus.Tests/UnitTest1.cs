using System;
using System.Collections.Immutable;
using GraphQLParser;
using GraphQLParser.AST;
using Xunit;

namespace Zeus.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            QuerySyntaxProcessor f = new QuerySyntaxProcessor(null);
            f.Foo("{ applications { name } }");
        }

        [Fact]
        public void Test2()
        {
            QuerySyntaxProcessor f = new QuerySyntaxProcessor(null);
            f.Foo("query x($a: String) { applications { name } }");
        }

        [Fact]
        public void Test3()
        {
            QuerySyntaxProcessor f = new QuerySyntaxProcessor(null);
            f.Foo("query x($a: String) { applications(a: $a) { name } }");
        }

        [Fact]
        public void Test4()
        {
            Source source = new Source(@"type Application { name: String }");
            Lexer lexer = new Lexer();
            Parser parser = new Parser(lexer);
            parser.Parse(source).Accept(new FooVisitor()); ;
        }
    }

    public class FooVisitor
        : SyntaxNodeWalker
    {
        private ImmutableStack<ASTNode> _path = ImmutableStack<ASTNode>.Empty;

        public override void Visit(ASTNode node)
        {
            ImmutableStack<ASTNode> previous = _path;

            if (node != null)
            {
                _path = previous.Push(node);    
            }

            base.Visit(node);

            _path = previous;
        }
    }
}
