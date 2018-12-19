using System.IO;
using System.Text;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Language
{
    public class QuerySyntaxRewriterTests
    {
        [Fact]
        public void RewriteEveryFieldOfTheQuery()
        {
            // arange
            DocumentNode document = Parser.Default.Parse(
                "{ foo { bar { baz } } }");

            // act
            DocumentNode rewritten = document
                .Rewrite<DirectiveQuerySyntaxRewriter, DirectiveNode>(
                    new DirectiveNode("upper"));

            // assert
            var content = new StringBuilder();
            var stringWriter = new StringWriter(content);
            var documentWriter = new DocumentWriter(stringWriter);
            var serializer = new QuerySyntaxSerializer();
            serializer.Visit(rewritten, documentWriter);
            content.ToString().Snapshot();
        }
    }
}
