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

        [Fact]
        public void QueryWithPrivateVariables()
        {
            // arrange
            DocumentNode query_a = Parser.Default.Parse(
                FileResource.Open("StitchingQueryWithUnion.graphql"));
            DocumentNode query_b = Parser.Default.Parse(
                FileResource.Open("StitchingQueryWithVariables.graphql"));

            // act
            var rewriter = new MergeQueryRewriter(Array.Empty<string>());
            rewriter.AddQuery(query_a, "_a");
            rewriter.AddQuery(query_b, "_b");
            DocumentNode document = rewriter.Merge();

            // assert
            QuerySyntaxSerializer.Serialize(document).Snapshot();
        }

        [Fact]
        public void QueryWithGlobalVariables()
        {
            // arrange
            DocumentNode query_a = Parser.Default.Parse(
                FileResource.Open("MergeQueryWithVariable.graphql"));
            DocumentNode query_b = Parser.Default.Parse(
                FileResource.Open("MergeQueryWithVariable.graphql"));

            // act
            var rewriter = new MergeQueryRewriter(Array.Empty<string>());
            rewriter.AddQuery(query_a, "_a");
            rewriter.AddQuery(query_b, "_b");
            DocumentNode document = rewriter.Merge();

            // assert
            QuerySyntaxSerializer.Serialize(document).Snapshot();
        }
    }
}
