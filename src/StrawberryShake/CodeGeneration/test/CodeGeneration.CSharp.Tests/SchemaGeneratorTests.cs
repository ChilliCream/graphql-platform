using ChilliCream.Testing;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class SchemaGeneratorTests
    {
        [Fact]
        public void Schema_With_Spec_Errors()
        {
            AssertResult(
                strictValidation: false,
                @"
                    query getListingsCount {
                        listings {
                        ... ListingsPayload
                        }
                    }
                    fragment ListingsPayload on ListingsPayload{
                        count
                    }
                ",
                FileResource.Open("BridgeClientDemo.graphql"),
                "extend schema @key(fields: \"id\")");
        }

        [Fact]
        public void Create_Query_With_Skip_Take()
        {
            AssertResult(
                @"query SearchNewsItems($query: String! $skip: Int $take: Int) {
                    newsItems(skip: $skip take: $take query: $query) {
                        items {
                            id
                            title
                            summary
                        }
                    }
                }",
                @"schema {
                    query: Query
                }

                type Query {
                    newsItems(skip: Int take: Int query: String!): NewsItemCollectionSegment
                }

                interface Node {
                    id: ID!
                }

                type NewsItem implements Node {
                    id: ID!
                    feedId: Uuid!
                    feedUrl: String!
                    html: String!
                    image: String!
                    keywords: [String!]!
                    language: String!
                    summary: String!
                    text: String!
                    title: String!
                    updated: DateTime
                    url: String!
                }

                type NewsItemCollectionSegment {
                    items: [NewsItem]
                    ""Information to aid in pagination.""
                    pageInfo: CollectionSegmentInfo!
                }

                ""Information about the offset pagination.""
                type CollectionSegmentInfo {
                    ""Indicates whether more items exist following the set defined by the clients arguments.""
                    hasNextPage: Boolean!
                    ""Indicates whether more items exist prior the set defined by the clients arguments.""
                    hasPreviousPage: Boolean!
                }",
                "extend schema @key(fields: \"id\")");
        }
    }
}
