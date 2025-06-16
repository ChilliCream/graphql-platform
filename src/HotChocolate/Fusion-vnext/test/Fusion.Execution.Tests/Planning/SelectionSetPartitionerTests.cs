using System.Collections.Immutable;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public class SelectionSetPartitionerTests
{
    [Fact]
    public void Extract_Name_Enqueue_Reviews()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = FusionSchemaDefinition.Create(compositeSchemaDoc);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ...ProductFragment1
                    ...ProductFragment2
                }
            }

            fragment ProductFragment1 on Product {
                name
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

        var fragmentRewriter = new InlineFragmentOperationRewriter(compositeSchema);
        var operation = fragmentRewriter.RewriteDocument(doc).Definitions.OfType<OperationDefinitionNode>().Single();
        var index = SelectionSetIndexer.Create(operation);

        // act
        var input = new SelectionSetPartitionerInput
        {
            SchemaName = "PRODUCTS",
            SelectionSet = new SelectionSet(
                index.GetId(operation.SelectionSet),
                operation.SelectionSet,
                compositeSchema.QueryType,
                SelectionPath.Root),
            SelectionSetIndex = index
        };
        var rewriter = new SelectionSetPartitioner(compositeSchema);
        var (resolvable, unresolvable, fields, _) = rewriter.Partition(input);

        // assert
        resolvable.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: $slug) {
                name
              }
            }
            """);

        Assert.Single(unresolvable);
    }

    [Fact]
    public void Extract_Name_Enqueue_Reviews_Enqueue_Name()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = FusionSchemaDefinition.Create(compositeSchemaDoc);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                a: productBySlug(slug: $slug) {
                    ...ProductFragment1
                    ...ProductFragment2
                }
                b: productBySlug(slug: $slug) {
                    ...ProductFragment1
                    ...ProductFragment2
                }
            }

            fragment ProductFragment1 on Product {
                name
                reviews {
                    nodes {
                        body
                        product {
                            name
                        }
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

        var fragmentRewriter = new InlineFragmentOperationRewriter(compositeSchema);
        var operation = fragmentRewriter.RewriteDocument(doc).Definitions.OfType<OperationDefinitionNode>().Single();
        var index = SelectionSetIndexer.Create(operation);

        // act
        var input = new SelectionSetPartitionerInput
        {
            SchemaName = "PRODUCTS",
            SelectionSet = new SelectionSet(
                index.GetId(operation.SelectionSet),
                operation.SelectionSet,
                compositeSchema.QueryType,
                SelectionPath.Root),
            SelectionSetIndex = index
        };
        var rewriter = new SelectionSetPartitioner(compositeSchema);
        var (resolvable, unresolvable, fields, _) = rewriter.Partition(input);

        // assert
        resolvable.MatchInlineSnapshot(
            """
            {
              a: productBySlug(slug: $slug) {
                name
              }
              b: productBySlug(slug: $slug) {
                name
              }
            }
            """);

        Assert.Equal(2, unresolvable.Count());
    }
}
