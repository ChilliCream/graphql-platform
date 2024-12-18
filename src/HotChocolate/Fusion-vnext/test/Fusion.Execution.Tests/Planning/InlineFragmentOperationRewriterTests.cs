using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public class InlineFragmentOperationRewriterTests
{
    [Test]
    public void Inline_Into_ProductById_SelectionSet()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);

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
        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
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

    [Test]
    public void Inline_Into_ProductById_SelectionSet_2_Levels()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);

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
        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
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

    [Test]
    public void Inline_Inline_Fragment_Into_ProductById_SelectionSet_1()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);

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
        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
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

    [Test]
    public void Inline_Into_ProductById_SelectionSet_3_Levels()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);

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
        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
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

    [Test]
    public void Do_Not_Inline_Inline_Fragment_Into_ProductById_SelectionSet()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);

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
        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
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

    [Test]
    public void Deduplicate_Fields()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);

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
        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
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

    [Test]
    public void Leafs_With_Different_Directives_Do_Not_Merge()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);

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
        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productById(id: 1) {
                id
                name @include(if: $skip)
                name @skip(if: $skip)
              }
            }
            """);
    }

    [Test]
    public void Composites_Without_Directives_Are_Merged()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);

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
        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query($slug: String!) {
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

    [Test]
    public void Merge_Fields_With_Aliases()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);

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
        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query($slug: String!) {
              productBySlug(slug: $slug) {
                a: name
                name
              }
            }
            """);
    }
}
