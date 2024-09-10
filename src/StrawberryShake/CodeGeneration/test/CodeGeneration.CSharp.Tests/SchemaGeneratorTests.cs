using ChilliCream.Testing;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp;

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
                    feedId: UUID!
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
    public void Create_GetFeatById()
    {
        AssertResult(
            @"query GetFeatById($id: UUID!) {
                    feats(where: {id: {eq: $id}}) {
                        items {
                            id,
                            name,
                            level,
                            details {
                                text
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

    [Fact]
    public void QueryInterference()
    {
        AssertResult(
            @"query GetFeatsPage(
                  $skip: Int!
                  $take: Int!
                  $searchTerm: String! = """"
                  $order: [FeatSortInput!] = [{ name: ASC }]
                ) {
                  feats(
                    skip: $skip
                    take: $take
                    order: $order
                    where: {
                      or: [
                        { name: { contains: $searchTerm } }
                        { traits: { some: { name: { contains: $searchTerm } } } }
                      ]
                    }
                  ) {
                    totalCount
                    items {
                      ...FeatsPage
                    }
                  }
                }

                fragment FeatsPage on Feat {
                  id
                  name
                  level
                  canBeLearnedMoreThanOnce
                  details {
                    text
                  }
                }",
            @"query GetFeatById($id: UUID!) {
                  feats(where: { id: { eq: $id } }) {
                    items {
                      ...FeatById
                    }
                  }
                }

                fragment FeatById on Feat {
                  id
                  name
                  level
                  special
                  trigger
                  details {
                    text
                  }
                  actionType {
                    name
                  }
                }",
            "extend schema @key(fields: \"id\")",
            FileResource.Open("Schema_Bug_1.graphql"));
    }

    [Fact]
    public void NodeTypenameCollision()
    {
        AssertResult(
            @"
                type Query {
                    node(id: ID!): Node
                    workspaces: [Workspace!]!
                }

                interface Node {
                    id: ID!
                }

                type Workspace implements Node {
                    id: ID!
                    name: String!
                    url: String!
                    workspaceId: String!
                    description: String
                }
                ",
            @"
                query Nodes($id: ID!) {
                    node(id: $id) {
                        __typename
                        id
                    }
                }",
            "extend schema @key(fields: \"id\")");
    }

    [Fact]
    public void Full_Extension_File()
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
            @"scalar _KeyFieldSet

                directive @key(fields: _KeyFieldSet!) on SCHEMA | OBJECT

                directive @serializationType(name: String!) on SCALAR

                directive @runtimeType(name: String!) on SCALAR

                directive @enumValue(value: String!) on ENUM_VALUE

                directive @rename(name: String!) on INPUT_FIELD_DEFINITION | INPUT_OBJECT | ENUM | ENUM_VALUE

                extend schema @key(fields: ""id"")");
    }

    [Fact]
    public void NonNullLists()
    {
        AssertResult(
            @"
                query getAll {
                  listings {
                    ...Offer
                  }
                }
                fragment Offer on Offer {
                   amenities1
                   amenities2
                   amenities3
                   amenities4
                   amenities5
                   amenities6
                   amenities7
                }
                ",
            @"
                schema {
                  query: Query
                  mutation: null
                  subscription: null
                }
                type Query {
                  listings: [Listing!]!
                }
                interface Listing{
                  listingId: ID!
                }
                type Offer implements Listing{
                  listingId: ID!
                  amenities1: [Amenity!]!
                  amenities2: [Amenity!]
                  amenities3: [Amenity]!
                  amenities4: [Amenity]
                  amenities5: [[Amenity!]!]!
                  amenities6: [[Amenity!]!]
                  amenities7: [[Amenity!]]!
                }
                enum Amenity {
                  ITEM1
                  ITEM2
                }",
            "extend schema @key(fields: \"id\")");
    }

    [Fact]
    public void MultiLineDocumentation()
    {
        AssertResult(
            @"query Foo {
                    abc
                }",
            @"type Query {
                    """"""
                    ABC
                    DEF
                    """"""
                    abc: String
                }");
    }

    [Fact]
    public void IntrospectionQuery()
    {
        AssertResult(
            FileResource.Open("IntrospectionQuery.graphql"),
            @"type Query {
                    abc: String
                }");
    }

    [Fact]
    public void FieldsWithUnderlineInName()
    {
        AssertResult(
            @"
                    query GetBwr_TimeSeries(
                      $where: bwr_TimeSeriesFilterInput
                      $readDataInput: ReadDataInput!
                    ) {
                      bwr_TimeSeries(where: $where) {
                        nodes {
                          ...Bwr_TimeSeries
                        }
                      }
                    }

                    fragment Bwr_TimeSeries on bwr_TimeSeries {
                      inventoryId: _inventoryItemId
                      area
                      source
                      type
                      name
                      category
                      specification
                      commodity
                      resolution {
                        timeUnit
                        factor
                      }
                      unit
                      validationCriteria {
                        ...Bwr_ValidationCriteria
                      }
                      importSpecification {
                        fromPeriod
                        toPeriod
                      }
                      _dataPoints(input: $readDataInput) {
                        timestamp
                        value
                        flag
                      }
                    }

                    fragment Bwr_ValidationCriteria on bwr_ValidationCriteria {
                      _inventoryItemId
                      name
                      completeness
                      lowerBound
                      upperBound
                    }
                ",
            "extend schema @key(fields: \"id\")",
            FileResource.Open("FieldsWithUnderlinePrefix.graphql"));
    }

    [Fact]
    public void HasuraMutation()
    {
        AssertResult(
            @"
                     mutation insertPeople($people: [people_insert_input!]!) {
                        insert_people(objects: $people)
                        {
                            affected_rows
                        }
                    }
                ",
            "extend schema @key(fields: \"id\")",
            FileResource.Open("HasuraSchema.graphql"));
    }

    [Fact]
    public void LowerCaseScalarArgument()
    {
        AssertResult(
            @"
                    query GetPeopleByPk($id: uuid!) {
                        people_by_pk(id: $id) {
                            id
                            firstName
                            lastName
                        }
                    }
                ",
            "extend schema @key(fields: \"id\")",
            FileResource.Open("HasuraSchema.graphql"));
    }

    [Fact]
    public void EnumWithUnderscorePrefixedValues()
    {
        AssertResult(
            """
            schema {
                query: Query
            }

            type Query {
                field1: Enum1
            }

            enum Enum1 {
                _a   # -> "_A"
                _a_b # -> "_AB"
                _1   # -> "_1"
                _1_2 # -> "_12"
                __2  # -> "_2"
            }
            """,
            """
            query GetField1 {
                field1
            }
            """);
    }
}
