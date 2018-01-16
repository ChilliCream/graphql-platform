using GraphQLParser;
using Xunit;
using Zeus.Execution;

namespace Zeus.Tests
{
    public class DocumentTests
    {
        [Fact]
        public void Foo()
        {
            // arrange
            Source source = new Source("query a($b: String) { c(b: $b) { d } } query x($b: String) { c(b: $b) { d } }");
            Lexer lexer = new Lexer();
            Parser parser = new Parser(lexer);

            // act
            DocumentSyntaxVisitor visitor = new DocumentSyntaxVisitor();
            parser.Parse(source).Accept(visitor);
        }
    }
}