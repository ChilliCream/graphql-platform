using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Shared;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using static HotChocolate.Language.Utf8GraphQLParser;
using HttpClientConfiguration = HotChocolate.Fusion.Composition.HttpClientConfiguration;

namespace HotChocolate.Fusion;

public class RequestPlannerTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Same_Field_On_Two_Subgraphs_One_Removes_It()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            interface Node {
              id: ID!
            }

            type BlogAuthor {
              fullName: String!
            }

            type Query {
              node(id: ID!): Node
            }

            type User implements Node {
              followedBlogAuthors(first: Int!): [BlogAuthor]!
              someField: String!
              otherField: Int!
              anotherField: Float!
              id: ID!
            }
            """,
            """
            schema @remove(coordinate: "User.followedBlogAuthors") {
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            interface Node {
              id: ID!
            }

            type BlogAuthor {
              fullName: String!
            }

            type Query {
              node(id: ID!): Node
            }

            type User implements Node {
              followedBlogAuthors(first: Int!): [BlogAuthor]!
              id: ID!
            }
            """
        );

        var subgraphC = await TestSubgraph.CreateAsync(
            """
            type Query {
              userBySlug(slug: String!): User
            }

            type User {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB, subgraphC]);
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query {
              userBySlug(slug: "me") {
                ...likedAuthors
              }
            }

            fragment likedAuthors on User {
              someField
              otherField
              anotherField
              followedBlogAuthors(first: 3) {
                fullName
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Fragment_Deduplication_1()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              entry: SomeObject!
            }

            type SomeObject {
              id: ID!
              string: String!
              other: AnotherObject!
            }

            type AnotherObject {
              id: ID!
              number: Int!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query {
              entry {
                id
                string
                other {
                  __typename
                  ...frag4
                }
                ...frag1
                ...frag2
                ...frag3
              }
            }

            fragment frag1 on SomeObject {
              id
              string
              other {
                number
              }
            }

            fragment frag2 on SomeObject {
              id
              other {
                id
              }
            }

            fragment frag3 on SomeObject {
              id
              other {
                __typename
              }
            }

            fragment frag4 on AnotherObject {
              id
              number
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Fragment_Deduplication_2()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              unionField: SomeUnion!
            }

            union SomeUnion = Object1 | Object2

            type Object1 {
              someField: String
            }

            type Object2 {
              otherField: Int
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query {
              viewer {
                unionField {
                  ... on Object1 {
                    __typename
                    someField
                  }
                }
                unionField {
                  __typename
                }
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Fragment_Deduplication_3()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            schema {
              query: Query
            }

            "The node interface is implemented by entities that have a global unique identifier."
            interface Node {
              id: ID!
            }

            interface ProductFilter {
              identifier: String!
              title: String!
              tooltip: FilterTooltip
            }

            type AlternativeQuerySuggestion {
              queryString: String!
              productCount: Int!
              productPreviewImageUrls: [URL!]!
            }

            type BlogPage implements Node {
              id: ID!
            }

            type Brand implements Node {
              products(sortOrder: ProductListSortOrder! = RELEVANCE filters: [FilterInput!] "Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): BrandProductsConnection
              id: ID!
              databaseId: Int! @deprecated(reason: "This is only meant for backwards compatibility.")
            }

            "A connection to a list of items."
            type BrandProductFiltersConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [BrandProductFiltersEdge!]
              "A flattened list of the nodes."
              nodes: [ProductFilter!]
            }

            "An edge in a connection."
            type BrandProductFiltersEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: ProductFilter!
            }

            "A connection to a list of items."
            type BrandProductsConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [BrandProductsEdge!]
              "A flattened list of the nodes."
              nodes: [Product]
              "Identifies the total count of items in the connection."
              totalCount: Int!
              productFilters("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): BrandProductFiltersConnection
            }

            "An edge in a connection."
            type BrandProductsEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: Product
            }

            type CheckboxFilter implements ProductFilter {
              identifier: String!
              title: String!
              tooltip: FilterTooltip
              pinnedOptions: [CheckboxFilterOption!]!
              commonOptions: [CheckboxFilterOption!]!
            }

            type CheckboxFilterOption {
              optionIdentifier: String!
              title: String!
              count: Int!
              tooltip: FilterTooltip
            }

            type CommunityDiscussion implements Node {
              id: ID!
              databaseId: Int! @deprecated(reason: "This is only meant for backwards compatibility.")
            }

            type FilterTooltip {
              text: String
              absoluteUrl: URL
            }

            type GalaxusReferral {
              productCount: Int!
              portalUrl: URL!
              products: [Product!]
            }

            type NavigationItem implements Node {
              products(sortOrder: ProductListSortOrder! = RELEVANCE filters: [FilterInput!] "Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): NavigationItemProductsConnection
              id: ID!
            }

            "A connection to a list of items."
            type NavigationItemProductFiltersConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [NavigationItemProductFiltersEdge!]
              "A flattened list of the nodes."
              nodes: [ProductFilter!]
            }

            "An edge in a connection."
            type NavigationItemProductFiltersEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: ProductFilter!
            }

            "A connection to a list of items."
            type NavigationItemProductsConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [NavigationItemProductsEdge!]
              "A flattened list of the nodes."
              nodes: [Product]
              "Identifies the total count of items in the connection."
              totalCount: Int!
              productFilters("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): NavigationItemProductFiltersConnection
              quickFilters("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): NavigationItemQuickFiltersConnection
            }

            "An edge in a connection."
            type NavigationItemProductsEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: Product
            }

            "A connection to a list of items."
            type NavigationItemQuickFiltersConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [NavigationItemQuickFiltersEdge!]
              "A flattened list of the nodes."
              nodes: [QuickFilter!]
            }

            "An edge in a connection."
            type NavigationItemQuickFiltersEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: QuickFilter!
            }

            "Information about pagination in a connection."
            type PageInfo {
              "Indicates whether more edges exist following the set defined by the clients arguments."
              hasNextPage: Boolean!
              "Indicates whether more edges exist prior the set defined by the clients arguments."
              hasPreviousPage: Boolean!
              "When paginating backwards, the cursor to continue."
              startCursor: String
              "When paginating forwards, the cursor to continue."
              endCursor: String
            }

            type Product implements Node {
              id: ID!
            }

            type ProductQuestion implements Node {
              id: ID!
              databaseId: Int! @deprecated(reason: "This is only meant for backwards compatibility.")
            }

            type ProductReview implements Node {
              id: ID!
              databaseId: Int! @deprecated(reason: "This is only meant for backwards compatibility.")
            }

            type ProductType implements Node {
              id: ID!
              databaseId: Int! @deprecated(reason: "This is only meant for backwards compatibility.")
            }

            type Query {
              "Fetches an object given its ID."
              node("ID of the object." id: ID!): Node
              "Lookup nodes by a list of IDs."
              nodes("The list of node IDs." ids: [ID!]!): [Node]!
              productById(id: ID!): Product
              blogPageById(id: ID!): BlogPage
              brandById(id: ID!): Brand
              productTypeById(id: ID!): ProductType
              discussionById(id: ID!): CommunityDiscussion
              questionById(id: ID!): ProductQuestion
              ratingById(id: ID!): ProductReview
              navigationItemById(id: ID!): NavigationItem
              shopSearch(query: String! filters: [FilterInput!] searchQueryConfig: ShopSearchConfigInput): ShopSearchResult!
            }

            type QuickFilter {
              filterIdentifier: String!
              optionIdentifier: String!
              optionTitle: String!
              filterTitle: String!
              disabled: Boolean!
            }

            type RangeFilter implements ProductFilter {
              identifier: String!
              title: String!
              tooltip: FilterTooltip
              topOutliersMerged: Boolean!
              min: Decimal!
              max: Decimal!
              step: Float!
              unitName: String
              unitId: Int
              dataPoints: [RangeFilterDataPoint!]!
            }

            type RangeFilterDataPoint {
              count: Int!
              value: Decimal!
            }

            type ShopSearchAdditionalQueryInfo {
              correctedQuery: String
              didYouMeanQuery: String
              lastProductSearchPass: String
              executedSearchTerm: String
              testGroup: String
              isManagedQuery: Boolean!
              isRerankedQuery: Boolean!
            }

            type ShopSearchResult implements Node {
              products(sortOrder: ShopSearchSortOrder! = RELEVANCE filters: [FilterInput!] "Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): ShopSearchResultProductsConnection
              id: ID!
              productFilters("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): ShopSearchResultProductFiltersConnection
              quickFilters("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): ShopSearchResultQuickFiltersConnection
              productTypes("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): ShopSearchResultProductTypesConnection
              brands("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): ShopSearchResultBrandsConnection
              blogPages("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): ShopSearchResultBlogPagesConnection
              communityItems("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): ShopSearchResultCommunityItemsConnection
              additionalQueryInfo: ShopSearchAdditionalQueryInfo
              alternativeQuerySuggestions: [AlternativeQuerySuggestion!]
              "Certain queries lead to a redirection instead of a search result. A common case is a redirect to a brand page. Others are also possible."
              redirectionUrl: URL
              galaxusReferral: GalaxusReferral
            }

            "A connection to a list of items."
            type ShopSearchResultBlogPagesConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [ShopSearchResultBlogPagesEdge!]
              "A flattened list of the nodes."
              nodes: [BlogPage!]
            }

            "An edge in a connection."
            type ShopSearchResultBlogPagesEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: BlogPage!
            }

            "A connection to a list of items."
            type ShopSearchResultBrandsConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [ShopSearchResultBrandsEdge!]
              "A flattened list of the nodes."
              nodes: [Brand!]
            }

            "An edge in a connection."
            type ShopSearchResultBrandsEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: Brand!
            }

            "A connection to a list of items."
            type ShopSearchResultCommunityItemsConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [ShopSearchResultCommunityItemsEdge!]
              "A flattened list of the nodes."
              nodes: [CommunitySearchResult!]
            }

            "An edge in a connection."
            type ShopSearchResultCommunityItemsEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: CommunitySearchResult!
            }

            "A connection to a list of items."
            type ShopSearchResultProductFiltersConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [ShopSearchResultProductFiltersEdge!]
              "A flattened list of the nodes."
              nodes: [ProductFilter!]
            }

            "An edge in a connection."
            type ShopSearchResultProductFiltersEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: ProductFilter!
            }

            "A connection to a list of items."
            type ShopSearchResultProductTypesConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [ShopSearchResultProductTypesEdge!]
              "A flattened list of the nodes."
              nodes: [ProductType!]
            }

            "An edge in a connection."
            type ShopSearchResultProductTypesEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: ProductType!
            }

            "A connection to a list of items."
            type ShopSearchResultProductsConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [ShopSearchResultProductsEdge!]
              "A flattened list of the nodes."
              nodes: [Product]
              "Identifies the total count of items in the connection."
              totalCount: Int!
              productFilters("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): ShopSearchResultProductFiltersConnection
              quickFilters("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String): ShopSearchResultQuickFiltersConnection
            }

            "An edge in a connection."
            type ShopSearchResultProductsEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: Product
            }

            "A connection to a list of items."
            type ShopSearchResultQuickFiltersConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [ShopSearchResultQuickFiltersEdge!]
              "A flattened list of the nodes."
              nodes: [QuickFilter!]
            }

            "An edge in a connection."
            type ShopSearchResultQuickFiltersEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: QuickFilter!
            }

            union CommunitySearchResult = ProductReview | ProductQuestion | CommunityDiscussion

            input FilterInput {
              filterIdentifier: String!
              optionIdentifiers: [String!]
              min: Decimal
              max: Decimal
              unitId: Int
            }

            input ShopSearchConfigInput {
              searchQueryId: String
              ltrEnabled: Boolean
              rewriters: [String!]
              testGroup: String
            }

            enum ApplyPolicy {
              BEFORE_RESOLVER
              AFTER_RESOLVER
              VALIDATION
            }

            enum ProductListSortOrder {
              HIGHEST_PRICE
              LOWEST_PRICE
              NUMBER_OF_SALES
              RELEVANCE
              REBATE
              AVAILABILITY
              RATING
              NEWEST
            }

            enum ShopSearchSortOrder {
              HIGHEST_PRICE
              LOWEST_PRICE
              NUMBER_OF_SALES
              RELEVANCE
              REBATE
              AVAILABILITY
              RATING
              NEWEST
            }

            "The built-in `Decimal` scalar type."
            scalar Decimal

            "The `Long` scalar type represents non-fractional signed whole 64-bit numeric values. Long can represent values between -(2^63) and 2^63 - 1."
            scalar Long

            scalar URL @specifiedBy(url: "https:\/\/tools.ietf.org\/html\/rfc3986")
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query searchQuery(
              $query: String!
              $filters: [FilterInput!]
              $sortOrder: ShopSearchSortOrder!
              $searchQueryConfig: ShopSearchConfigInput!
            ) {
              shopSearch(query: $query, searchQueryConfig: $searchQueryConfig) {
                ...searchEngineResultsPage
                ...searchEngineResultsPageProducts_1rZpll
                products(first: 48, filters: $filters, sortOrder: $sortOrder) {
                  productFilters {
                    edges {
                      node {
                        __typename
                        identifier
                      }
                    }
                  }
                }
                id
              }
            }

            fragment alternativeQuerySuggestions on ShopSearchResult {
              alternativeQuerySuggestions {
                productCount
                productPreviewImageUrls
                queryString
              }
            }

            fragment galaxusReferral on ShopSearchResult {
              galaxusReferral {
                portalUrl
                productCount
                products {
                  id
                }
              }
            }

            fragment searchEngineBlogTeasersSection on ShopSearchResult {
              blogPages {
                nodes {
                  id
                }
              }
            }

            fragment searchEngineCommunitySection on ShopSearchResult {
              communityItems(first: 3) {
                nodes {
                  __typename
                  ... on CommunityDiscussion {
                    databaseId
                  }
                  ... on ProductQuestion {
                    databaseId
                  }
                  ... on ProductReview {
                    databaseId
                  }
                  ... on Node {
                    __isNode: __typename
                    id
                  }
                }
              }
            }

            fragment searchEngineProductsSection on ShopSearchResultProductsConnection {
              edges {
                node {
                  id
                }
              }
              productFilters {
                edges {
                  node {
                    __typename
                    ... on CheckboxFilter {
                      __typename
                      identifier
                      title
                    }
                    ... on RangeFilter {
                      __typename
                      identifier
                      title
                    }
                  }
                }
              }
              totalCount
              pageInfo {
                hasNextPage
                endCursor
              }
            }

            fragment searchEngineResultsPage on ShopSearchResult {
              ...searchEngineTitleSection
              ...useSerpTrackingShopSearchResult
              ...searchEngineBlogTeasersSection
              ...searchEngineCommunitySection
              ...zeroResults
              additionalQueryInfo {
                lastProductSearchPass
              }
              alternativeQuerySuggestions {
                __typename
              }
              galaxusReferral {
                __typename
              }
              redirectionUrl
            }

            fragment searchEngineResultsPageProducts_1rZpll on ShopSearchResult {
              products(first: 48, filters: $filters, sortOrder: $sortOrder) {
                edges {
                  node {
                    id
                    __typename
                  }
                  cursor
                }
                ...searchEngineProductsSection
                ...vectorProductsSection
                ...useSerpTrackingProducts
                productFilters {
                  ...searchFiltersProductFilters
                }
                quickFilters {
                  ...searchFiltersQuickFilters
                }
                pageInfo {
                  endCursor
                  hasNextPage
                }
              }
              id
            }

            fragment searchEngineTitleSection on ShopSearchResult {
              additionalQueryInfo {
                correctedQuery
                didYouMeanQuery
                lastProductSearchPass
              }
            }

            fragment searchFiltersProductFilters on ShopSearchResultProductFiltersConnection {
              edges {
                node {
                  __typename
                  ... on CheckboxFilter {
                    __typename
                    identifier
                    title
                    commonOptions {
                      count
                      optionIdentifier
                      title
                      tooltip {
                        absoluteUrl
                        text
                      }
                    }
                    pinnedOptions {
                      count
                      optionIdentifier
                      title
                      tooltip {
                        absoluteUrl
                        text
                      }
                    }
                    tooltip {
                      absoluteUrl
                      text
                    }
                  }
                  ... on RangeFilter {
                    __typename
                    identifier
                    title
                    min
                    max
                    step
                    dataPoints {
                      count
                      value
                    }
                    topOutliersMerged
                    unitId
                    unitName
                    tooltip {
                      absoluteUrl
                      text
                    }
                  }
                }
              }
            }

            fragment searchFiltersQuickFilters on ShopSearchResultQuickFiltersConnection {
              edges {
                node {
                  disabled
                  filterIdentifier
                  optionIdentifier
                  optionTitle
                  filterTitle
                }
              }
            }

            fragment useSerpTrackingProducts on ShopSearchResultProductsConnection {
              totalCount
            }

            fragment useSerpTrackingShopSearchResult on ShopSearchResult {
              brands {
                edges {
                  node {
                    id
                  }
                }
              }
              productTypes {
                edges {
                  node {
                    id
                  }
                }
              }
              redirectionUrl
              galaxusReferral {
                __typename
              }
              alternativeQuerySuggestions {
                queryString
              }
              additionalQueryInfo {
                correctedQuery
                didYouMeanQuery
                isRerankedQuery
                lastProductSearchPass
                testGroup
              }
            }

            fragment vectorProductsSection on ShopSearchResultProductsConnection {
              edges {
                node {
                  id
                }
              }
              pageInfo {
                hasNextPage
                endCursor
              }
              totalCount
            }

            fragment zeroResults on ShopSearchResult {
              ...alternativeQuerySuggestions
              ...galaxusReferral
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Fragment_Deduplication_4()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field1: String!
              field2: String!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query {
              field1
              ... on Query {
                field2
              }
              ...query
            }

            fragment query on Query {
              field1
              field2
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Fragment_Deduplication_5()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field1: String!
              field2: String!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query test($skip: Boolean!) {
              field1
              ... @skip(if: $skip) {
                field2
              }
              ...query
            }

            fragment query on Query {
              field1
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Fragment_Deduplication_6()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field1: String!
              field2: String!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query test($skip: Boolean!) {
              field1
              ... on Query @skip(if: $skip) {
                field2
              }
              ...query
            }

            fragment query on Query {
              field1
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_01()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query GetUser {
              users {
                name
                reviews {
                  body
                  author {
                    name
                  }
                }
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_02_Aliases()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query GetUser {
              a: users {
                name
                reviews {
                  body
                  author {
                    name
                  }
                }
              }
              b: users {
                name
                reviews {
                  body
                  author {
                    name
                  }
                }
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_03_Argument_Literals()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query GetUser {
              userById(id: 1) {
                id
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_04_Argument_Variables()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query TopProducts($first: Int!) {
                topProducts(first: $first) {
                    id
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_05_TypeName_Field()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query TopProducts {
                __typename
                topProducts(first: 2) {
                    __typename
                    reviews {
                        __typename
                        author {
                            __typename
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_06_Introspection()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Introspect {
                __schema {
                    types {
                        name
                        kind
                        fields {
                            name
                            type {
                                name
                                kind
                            }
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_07_Introspection_And_Fetch()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query TopProducts($first: Int!) {
                topProducts(first: $first) {
                    id
                }
                __schema {
                    types {
                        name
                        kind
                        fields {
                            name
                            type {
                                name
                                kind
                            }
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_08_Single_Mutation()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            mutation AddReview {
                addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
                    review {
                        body
                        author {
                            name
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_09_Two_Mutation_Same_SubGraph()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            mutation AddReviews {
                a: addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
                    review {
                        body
                        author {
                            name
                        }
                    }
                }
                b: addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
                    review {
                        body
                        author {
                            name
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_10_Two_Mutation_Same_SubGraph()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            mutation AddReviews {
                a: addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
                    review {
                        body
                        author {
                            birthdate
                        }
                    }
                }
                b: addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
                    review {
                        body
                        author {
                            id
                            birthdate
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_11_Two_Mutation_Two_SubGraph()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            mutation AddReviewAndUser {
                addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
                    review {
                        body
                        author {
                            id
                            birthdate
                        }
                    }
                }
                addUser(input: { name: "foo", username: "foo", birthdate: "abc" }) {
                    user {
                        name
                        reviews {
                            body
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_12_Subscription_1()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            subscription OnNewReview {
                onNewReview {
                    body
                    author {
                        name
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_13_Subscription_2()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            subscription OnNewReview {
                onNewReview {
                    body
                    author {
                        name
                        birthdate
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_14_Node_Single_Fragment()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query FetchNode($id: ID!) {
                node(id: $id) {
                    ... on User {
                        id
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_15_Node_Single_Fragment_Multiple_Subgraphs()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query FetchNode($id: ID!) {
                node(id: $id) {
                    ... on User {
                        birthdate
                        reviews {
                            body
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_16_Two_Node_Fields_Aliased()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query FetchNode($a: ID! $b: ID!) {
                a: node(id: $a) {
                    ... on User {
                        id
                    }
                }
                b: node(id: $b) {
                    ... on User {
                        id
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_17_Multi_Completion()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query GetUser {
              users {
                birthdate
              }
              reviews {
                body
              }
              __schema {
                types {
                  name
                }
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_18_Node_Single_Fragment_Multiple_Subgraphs()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query FetchNode($id: ID!) {
                node(id: $id) {
                    ... on User {
                        birthdate
                        reviews {
                            body
                        }
                    }
                    ... on Review {
                        body
                        author {
                            birthdate
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_19_Requires()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Requires {
                reviews {
                  body
                  author {
                    name
                    birthdate
                  }
                  product {
                    name
                    deliveryEstimate(zip: "12345") {
                      min
                      max
                    }
                  }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_20_DeepQuery()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query GetUser {
              users {
                name
                reviews {
                  body
                  author {
                    name
                    birthdate
                    reviews {
                      body
                      author {
                        name
                        birthdate
                      }
                    }
                  }
                }
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_21_Field_Requirement_Not_In_Context()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Requires {
                reviews {
                  body
                  author {
                    name
                    birthdate
                  }
                  product {
                    deliveryEstimate(zip: "12345") {
                      min
                      max
                    }
                  }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_22_Interfaces()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Appointment.ToConfiguration(),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Appointments {
              appointments {
                nodes {
                  patient {
                    id
                  }
                }
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_23_Interfaces_Merge()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Appointment.ToConfiguration(),
                demoProject.Patient1.ToConfiguration(),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Appointments {
              appointments {
                nodes {
                  patient {
                    id
                    ... on Patient1 {
                        name
                    }
                  }
                }
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_24_Field_Requirement_And_Fields_In_Context()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Requires {
                reviews {
                  body
                  author {
                    name
                    birthdate
                  }
                  product {
                    id
                    name
                    deliveryEstimate(zip: "12345") {
                      min
                      max
                    }
                  }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_25_Variables_Are_Passed_Through()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Appointment.ToConfiguration(),
                demoProject.Patient1.ToConfiguration(),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Appointments($first: Int!) {
              patientById(patientId: 1) {
                name
                appointments(first: $first) {
                    nodes {
                        id
                    }
                }
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_26_Ensure_No_Circular_Dependency_When_Requiring_Data()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query TopProducts {
              topProducts(first: 5) {
                weight
                deliveryEstimate(zip: "12345") {
                  min
                  max
                }
                reviews {
                  body
                }
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_27_Multiple_Require_Steps_From_Same_Subgraph()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Authors.ToConfiguration(),
                demoProject.Books.ToConfiguration(),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Query {
                authorById(id: "1") {
                    id,
                    name,
                    bio,
                    books {
                        id
                        author {
                            books {
                                id
                            }
                        }
                    }
                }
            }
            """);

        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_28_Simple_Root_Data()
    {
        // arrange
        var schemaA =
            """
            type Query {
                data: Data
            }

            type Data {
                a: String
            }

            schema {
                query: Query
            }
            """;

        var schemaB =
            """
            type Query {
                data: Data
            }

            type Data {
                b: String
            }

            schema {
                query: Query
            }
            """;

        var fusionGraph = await FusionGraphComposer.ComposeAsync(new[]
        {
            new SubgraphConfiguration("A", schemaA, Array.Empty<string>(), CreateClients(), null),
            new SubgraphConfiguration("B", schemaB, Array.Empty<string>(), CreateClients(), null),
        });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Query {
                data {
                    a
                    b
                }
            }
            """);

        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_29_Simple_Root_List_Data()
    {
        // arrange
        var schemaA =
            """
            type Query {
                data: [Data]
            }

            type Data {
                a: String
            }

            schema {
                query: Query
            }
            """;

        var schemaB =
            """
            type Query {
                data: [Data]
            }

            type Data {
                b: String
            }

            schema {
                query: Query
            }
            """;

        var fusionGraph = await FusionGraphComposer.ComposeAsync(new[]
        {
            new SubgraphConfiguration("A", schemaA, Array.Empty<string>(), CreateClients(), null),
            new SubgraphConfiguration("B", schemaB, Array.Empty<string>(), CreateClients(), null),
        });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Query {
                data {
                    a
                    b
                }
            }
            """);

        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_30_Entity_Data()
    {
        // arrange
        var schemaA =
            """
            type Query {
                entity(id: ID!): Entity
            }

            type Entity {
                id: ID!
                a: String
            }

            schema {
                query: Query
            }
            """;

        var schemaB =
            """
            type Query {
                entity(id: ID!): Entity
            }

            type Entity {
                id: ID!
                b: String
            }

            schema {
                query: Query
            }
            """;

        var fusionGraph = await FusionGraphComposer.ComposeAsync(new[]
        {
            new SubgraphConfiguration("A", schemaA, Array.Empty<string>(), CreateClients(), null),
            new SubgraphConfiguration("B", schemaB, Array.Empty<string>(), CreateClients(), null),
        });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Query {
                entity(id: 123) {
                    a
                    b
                }
            }
            """);

        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_32_Argument_No_Value_Specified()
    {
        // arrange
        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                new SubgraphConfiguration(
                    "Test",
                    """
                    type Query {
                        fieldWithEnumArg(arg: TestEnum = VALUE2): Boolean
                    }

                    enum TestEnum {
                        VALUE1,
                        VALUE2
                    }
                    """,
                    "",
                    new []
                    {
                        new HttpClientConfiguration(new Uri("http://client"), "Test"),
                    },
                    null),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Test {
              fieldWithEnumArg
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_33_Argument_Default_Value_Explicitly_Specified()
    {
        // arrange
        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                new SubgraphConfiguration(
                    "Test",
                    """
                    type Query {
                        fieldWithEnumArg(arg: TestEnum = VALUE2): Boolean
                    }

                    enum TestEnum {
                        VALUE1,
                        VALUE2
                    }
                    """,
                    "",
                    new []
                    {
                        new HttpClientConfiguration(new Uri("http://client"), "Test"),
                    },
                    null),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Test {
              fieldWithEnumArg(arg: VALUE2)
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_34_Argument_Not_Default_Value_Specified()
    {
        // arrange
        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                new SubgraphConfiguration(
                    "Test",
                    """
                    type Query {
                        fieldWithEnumArg(arg: TestEnum = VALUE2): Boolean
                    }

                    enum TestEnum {
                        VALUE1,
                        VALUE2
                    }
                    """,
                    "",
                    new []
                    {
                        new HttpClientConfiguration(new Uri("http://client"), "Test"),
                    },
                    null),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Test {
              fieldWithEnumArg(arg: VALUE1)
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_35_Argument_Value_Variable_Specified()
    {
        // arrange
        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                new SubgraphConfiguration(
                    "Test",
                    """
                    type Query {
                        fieldWithEnumArg(arg: TestEnum = VALUE2): Boolean
                    }

                    enum TestEnum {
                        VALUE1,
                        VALUE2
                    }
                    """,
                    "",
                    new []
                    {
                        new HttpClientConfiguration(new Uri("http://client"), "Test"),
                    },
                    null),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Test($variable: TestEnum) {
              fieldWithEnumArg(arg: $variable)
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_31_Argument_No_Value_Specified_With_Selection_Set()
    {
        // arrange
        var fusionGraph = await FusionGraphComposer.ComposeAsync(
        [
            new SubgraphConfiguration(
                    "Test",
                    """
                    type Query {
                        fieldWithEnumArg(arg: TestEnum = VALUE2): TestObject
                    }

                    type TestObject {
                        test: Boolean
                    }

                    enum TestEnum {
                        VALUE1,
                        VALUE2
                    }
                    """,
                    "",
                    [
                        new HttpClientConfiguration(new Uri("http://client"), "Test")
                    ],
                    null)
        ]);

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Test {
              fieldWithEnumArg {
                test
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_36_Requires_CommonField_Multiple_Times()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Requires {
                users {
                  id
                  username
                  productConfigurationByUsername {
                    id
                  }
                  productBookmarkByUsername {
                    id
                  }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Query_Plan_37_Requires_CommonField_Once()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Requires {
                users {
                  id
                  username
                  productConfigurationByUsername {
                    id
                  }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    private static async Task<(DocumentNode UserRequest, Execution.Nodes.QueryPlan QueryPlan)> CreateQueryPlanAsync(
        Skimmed.SchemaDefinition fusionGraph,
        [StringSyntax("graphql")] string query)
    {
        var document = SchemaFormatter.FormatAsDocument(fusionGraph);
        var context = FusionTypeNames.From(document);
        var rewriter = new FusionGraphConfigurationToSchemaRewriter();
        var rewritten = rewriter.Rewrite(document, new(context))!;

        var services = new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(rewritten.ToString())
            .UseField(n => n);

        if (document.Definitions.Any(d => d is ScalarTypeDefinitionNode { Name.Value: "Upload", }))
        {
            services.AddUploadType();
        }

        var schema = await services.BuildSchemaAsync();
        var serviceConfig = FusionGraphConfiguration.Load(document);

        var request = Parse(query);

        var operationCompiler = new OperationCompiler(new());
        var operationDef = (OperationDefinitionNode)request.Definitions[0];
        var operation = operationCompiler.Compile(
            new OperationCompilerRequest(
                "abc",
                request,
                operationDef,
                schema.GetOperationType(operationDef.Operation)!,
                schema));

        var queryPlanner = new QueryPlanner(serviceConfig, schema);
        var queryPlan = queryPlanner.Plan(operation);

        return (request, queryPlan);
    }

    private static IClientConfiguration[] CreateClients()
        =>
        [
            new HttpClientConfiguration(new Uri("http://nothing")),
        ];
}
