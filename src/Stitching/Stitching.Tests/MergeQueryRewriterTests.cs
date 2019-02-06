using System;
using ChilliCream.Testing;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Stitching
{
    public class MergeQueryRewriterTests
    {
        [Fact]
        public void SimpleShortHandQuery()
        {
            // arrange
            string query_a = "{ a { b } }";
            string query_b = "{ c { d } }";
            string query_c = "{ a { c } }";

            // act
            var rewriter = new MergeQueryRewriter(Array.Empty<string>());
            rewriter.AddQuery(Parser.Default.Parse(query_a), "_a");
            rewriter.AddQuery(Parser.Default.Parse(query_b), "_b");
            rewriter.AddQuery(Parser.Default.Parse(query_c), "_c");
            DocumentNode document = rewriter.Merge();

            // assert
            QuerySyntaxSerializer.Serialize(document).Snapshot();
        }
    }
}
