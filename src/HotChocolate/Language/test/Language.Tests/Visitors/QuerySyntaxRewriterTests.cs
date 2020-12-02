using System.IO;
using System.Text;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class QuerySyntaxRewriterTests
    {
        [Fact]
        public void RewriteEveryFieldOfTheQuery()
        {
            // arange
            DocumentNode document = Utf8GraphQLParser.Parse(
                "{ foo { bar { baz } } }");

            // act
            DocumentNode rewritten = document
                .Rewrite<DirectiveQuerySyntaxRewriter, DirectiveNode>(
                    new DirectiveNode("upper"));

            // assert
            rewritten.ToString().MatchSnapshot();
        }
    }
}
