using System;
using System.Collections.Generic;
using System.Linq;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Stitching.Utilities;
using Snapshooter.Xunit;
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
            var rewriter = new MergeRequestRewriter(Array.Empty<string>());
            rewriter.AddQuery(Utf8GraphQLParser.Parse(query_a), "_a", false);
            rewriter.AddQuery(Utf8GraphQLParser.Parse(query_b), "_b", false);
            rewriter.AddQuery(Utf8GraphQLParser.Parse(query_c), "_c", false);
            DocumentNode document = rewriter.Merge();

            // assert
            QuerySyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void QueryWithPrivateVariables()
        {
            // arrange
            DocumentNode query_a = Utf8GraphQLParser.Parse(
                FileResource.Open("StitchingQueryWithUnion.graphql"));
            DocumentNode query_b = Utf8GraphQLParser.Parse(
                FileResource.Open("StitchingQueryWithVariables.graphql"));

            // act
            var rewriter = new MergeRequestRewriter(Array.Empty<string>());
            rewriter.AddQuery(query_a, "_a", false);
            rewriter.AddQuery(query_b, "_b", false);
            DocumentNode document = rewriter.Merge();

            // assert
            QuerySyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void QueryWithGlobalVariables()
        {
            // arrange
            DocumentNode query_a = Utf8GraphQLParser.Parse(
                FileResource.Open("MergeQueryWithVariable.graphql"));
            DocumentNode query_b = Utf8GraphQLParser.Parse(
                FileResource.Open("MergeQueryWithVariable.graphql"));

            // act
            var rewriter = new MergeRequestRewriter(
                new HashSet<string>(new[] { "global" }));
            rewriter.AddQuery(query_a, "_a", true);
            rewriter.AddQuery(query_b, "_b", true);
            DocumentNode document = rewriter.Merge();

            // assert
            QuerySyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void AliasesMapIsCorrect()
        {
            // arrange
            DocumentNode query_a = Utf8GraphQLParser.Parse(
                FileResource.Open("MergeQueryWithVariable.graphql"));
            DocumentNode query_b = Utf8GraphQLParser.Parse(
                FileResource.Open("MergeQueryWithVariable.graphql"));

            // act
            var rewriter = new MergeRequestRewriter(Array.Empty<string>());
            IDictionary<string, string> a =
                rewriter.AddQuery(query_a, "_a", true);
            IDictionary<string, string> b =
                rewriter.AddQuery(query_b, "_b", true);

            // assert
            a.MatchSnapshot("AliasesMapIsCorrect_A");
            b.MatchSnapshot("AliasesMapIsCorrect_B");
        }


        [Fact]
        public void DocumentHasNoOperation()
        {
            // arrange
            DocumentNode query = Utf8GraphQLParser.Parse(
                "type Foo { s: String }");

            // act
            var rewriter = new MergeRequestRewriter(Array.Empty<string>());
            Action action = () => rewriter.AddQuery(query, "_a", false);

            // assert
            Assert.Equal("document",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void DocumentIsNull()
        {
            // arrange
            DocumentNode query = Utf8GraphQLParser.Parse(
                "type Foo { s: String }");

            // act
            var rewriter = new MergeRequestRewriter(Array.Empty<string>());
            Action action = () => rewriter.AddQuery(null, "_a", false);

            // assert
            Assert.Equal("document",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void QueriesAreNotOfTheSameOperationType()
        {
            // arrange
            DocumentNode query_a = Utf8GraphQLParser.Parse("query a { b }");
            DocumentNode query_b = Utf8GraphQLParser.Parse("mutation a { b }");

            // act
            var rewriter = new MergeRequestRewriter(Array.Empty<string>());
            rewriter.AddQuery(query_a, "abc", false);
            Action action = () => rewriter.AddQuery(query_b, "abc", false);

            // assert
            Assert.Equal("document",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void RequestPrefixIsEmpty()
        {
            // arrange
            DocumentNode query = Utf8GraphQLParser.Parse(
                "type Foo { s: String }");

            // act
            var rewriter = new MergeRequestRewriter(Array.Empty<string>());
            Action action = () => rewriter.AddQuery(
                query, default(NameString), false);

            // assert
            Assert.Equal("requestPrefix",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void CreateNewInstance_GlobalVariablesIsNull()
        {
            // arrange
            DocumentNode query = Utf8GraphQLParser.Parse(
                "type Foo { s: String }");

            // act
            Action action = () => new MergeRequestRewriter(null);

            // assert
            Assert.Equal("globalVariableNames",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }
    }
}
