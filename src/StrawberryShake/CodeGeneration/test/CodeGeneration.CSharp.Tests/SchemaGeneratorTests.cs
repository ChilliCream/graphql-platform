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
        public void Query_With_Nested_Fragments()
        {
            AssertResult(
                strictValidation: true,
                @"
                    query getAll(){
                        listings{
                            ... ListingsPayload
                        }
                    }
                    fragment ListingsPayload on ListingsPayload{
                        items{
                            ... HasListingId
                            ... Offer
                            ... Auction
                        }
                    }
                    fragment HasListingId on Listing{
                        listingId
                    }
                    fragment Offer on Offer{
                        price
                    }
                    fragment Auction on Auction{
                        startingPrice
                    }
                ",
                FileResource.Open("MultipleInterfaceSchema.graphql"),
                "extend schema @key(fields: \"id\")");
        }

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

        [Fact]
        public void Create_PeopleSearch_From_ActiveDirectory_Schema()
        {
            AssertResult(
                @"query PeopleSearch($term:String! $skip:Int $take:Int $inactive:Boolean) {
                  people: peopleSearch(
                    term: $term
                    includeInactive: $inactive
                    skip: $skip
                    take: $take
                  )
                  {
                    totalCount
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                    items {
                      ...PeopleSearchResult
                    }
                  }
                }

                fragment PeopleSearchResult on Person {
                  id
                  key
                  displayName
                  isActive
                  department {
                    id
                    name
                  }
                  image
                  title
                  manager {
                    id
                    key
                    displayName
                  }
                }",
                "extend schema @key(fields: \"id\")",
                FileResource.Open("ActiveDirectory.Schema.graphql"));
        }

        [Fact]
        public void Create_GetFeatsPage()
        {
            AssertResult(
                @"query GetFeatsPage($skip: Int, $take: Int) {
                    feats(skip: $skip, take: $take) {
                        items {
                            name,
                            level,
                            canBeLearnedMoreThanOnce,
                            actionType {
                                name
                            }
                        }
                    }
                }",
                "extend schema @key(fields: \"id\")",
                FileResource.Open("Schema_Bug_1.graphql"));
        }

        [Fact]
        public void Create_DataType_Query()
        {
            AssertResult(
                @"query GetAllFoos {
                    test {
                        profile {
                            name
                        }
                    }
                }",
                "extend schema @key(fields: \"id\")",
                @"schema {
                    query: Query
                }

                type Query {
                    test: [Foo!]!
                }

                type Foo {
                    profile: Profile!
                }

                type Profile {
                    # id: ID! # Can no longer generate if no id is present
                    name: String
                }");
        }

        [Fact]
        public void Create_UpdateMembers_Mutation()
        {
            AssertResult(
                @"mutation UpdateMembers($input: UpdateProjectMembersInput!) {
                    project {
                        updateMembers(input: $input) {
                            correlationId
                        }
                    }
                }",
                "extend schema @key(fields: \"id\")",
                FileResource.Open("Schema_Bug_2.graphql"));
        }
    }
}
