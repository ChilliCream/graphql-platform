using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion.Rewriters;

public class InlineFragmentOperationRewriterTests
{
    [Fact]
    public void Inline_Into_ProductById_SelectionSet()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        // act
        var rewriter = new InlineFragmentOperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);
    }

    [Fact]
    public void Inline_Into_ProductById_SelectionSet_2_Levels()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    ... Product1
                }
            }

            fragment Product1 on Product {
                ... Product2
            }

            fragment Product2 on Product {
                id
                name
            }
            """);

        // act
        var rewriter = new InlineFragmentOperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);
    }

    [Fact]
    public void Inline_Inline_Fragment_Into_ProductById_SelectionSet_1()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    ... {
                        id
                        name
                    }
                }
            }
            """);

        // act
        var rewriter = new InlineFragmentOperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);
    }

    [Fact]
    public void Inline_Into_ProductById_SelectionSet_3_Levels()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    ... on Product {
                        ... Product1
                    }
                }
            }

            fragment Product1 on Product {
                ... Product2
            }

            fragment Product2 on Product {
                id
                name
            }
            """);

        // act
        var rewriter = new InlineFragmentOperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);
    }

    [Fact]
    public void Do_Not_Inline_Inline_Fragment_Into_ProductById_SelectionSet()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    id
                    ... @include(if: true) {
                        id
                        name
                    }
                }
            }
            """);

        // act
        var rewriter = new InlineFragmentOperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
                ... @include(if: true) {
                  id
                  name
                }
              }
            }
            """);
    }

    [Fact]
    public void Deduplicate_Fields()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    ... Product
                    name
                }
            }

            fragment Product on Product {
                id
                name
                name
            }
            """);

        // act
        var rewriter = new InlineFragmentOperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);
    }

    [Fact]
    public void Leafs_With_Different_Directives_Do_Not_Merge()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
                productById(id: 1) {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name @include(if: $skip)
                name @include(if: $skip)
                name @skip(if: $skip)
                name @skip(if: $skip)
            }
            """);

        // act
        var rewriter = new InlineFragmentOperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              productById(id: 1) {
                id
                name @include(if: $skip)
                name @skip(if: $skip)
              }
            }
            """);
    }

    [Fact]
    public void Composites_Without_Directives_Are_Merged()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ...ProductFragment1
                    ...ProductFragment2
                }
            }

            fragment ProductFragment1 on Product {
                reviews {
                    nodes {
                        body
                    }
                }
            }

            fragment ProductFragment2 on Product {
                reviews {
                    pageInfo {
                        hasNextPage
                    }
                }
            }
            """);

        // act
        var rewriter = new InlineFragmentOperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $slug: String!
            ) {
              productBySlug(slug: $slug) {
                reviews {
                  nodes {
                    body
                  }
                  pageInfo {
                    hasNextPage
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Merge_Fields_With_Aliases()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                   ... {
                       a: name
                   }
                   a: name
                   name
                   ... {
                       name
                   }
                }
            }
            """);

        // act
        var rewriter = new InlineFragmentOperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $slug: String!
            ) {
              productBySlug(slug: $slug) {
                a: name
                name
              }
            }
            """);
    }
}
