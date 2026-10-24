using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilitySharedRootFieldTests
{
    [Theory]
    [InlineData("A", "B", true)]
    [InlineData("A", "B", false)]
    [InlineData("B", "A", true)]
    [InlineData("B", "A", false)]
    public void Validate_Should_Succeed_When_NestedValueTypeIsSplitAcrossSchemasSharingRootField(
        string visitsSchemaName,
        string revenueSchemaName,
        bool sharedFieldFirst)
    {
        // arrange
        var (firstVisitsField, secondVisitsField) = sharedFieldFirst
            ? ("period: Period @shareable", "visits: Int")
            : ("visits: Int", "period: Period @shareable");
        var (firstRevenueField, secondRevenueField) = sharedFieldFirst
            ? ("period: Period @shareable", "revenue: Int")
            : ("revenue: Int", "period: Period @shareable");

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                visitsSchemaName,
                $$"""
                type Query {
                    dashboard: Dashboard @shareable
                }

                type Dashboard {
                    summary: Summary @shareable
                }

                type Summary {
                    {{firstVisitsField}}
                    {{secondVisitsField}}
                }

                type Period {
                    start: String
                }
                """),
            new SourceSchemaText(
                revenueSchemaName,
                $$"""
                type Query {
                    dashboard: Dashboard @shareable
                }

                type Dashboard {
                    summary: Summary @shareable
                }

                type Summary {
                    {{firstRevenueField}}
                    {{secondRevenueField}}
                }

                type Period {
                    end: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData("A", "B", true)]
    [InlineData("A", "B", false)]
    [InlineData("B", "A", true)]
    [InlineData("B", "A", false)]
    public void Validate_Should_Succeed_When_InterfaceImplementationIsSplitAcrossSchemasSharingRootField(
        string imageSchemaName,
        string linkSchemaName,
        bool sharedFieldFirst)
    {
        // arrange
        var (firstImageField, secondImageField) = sharedFieldFirst
            ? ("label: String @shareable", "imageUrl: String")
            : ("imageUrl: String", "label: String @shareable");
        var (firstLinkField, secondLinkField) = sharedFieldFirst
            ? ("label: String @shareable", "linkUrl: String")
            : ("linkUrl: String", "label: String @shareable");

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                imageSchemaName,
                $$"""
                type Query {
                    storefront: Storefront @shareable
                }

                type Storefront {
                    highlight: Highlight @shareable
                }

                interface Highlight {
                    label: String
                }

                type Banner implements Highlight {
                    {{firstImageField}}
                    {{secondImageField}}
                }
                """),
            new SourceSchemaText(
                linkSchemaName,
                $$"""
                type Query {
                    storefront: Storefront @shareable
                }

                type Storefront {
                    highlight: Highlight @shareable
                }

                interface Highlight {
                    label: String
                }

                type Banner implements Highlight {
                    {{firstLinkField}}
                    {{secondLinkField}}
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData("A", "B", true)]
    [InlineData("A", "B", false)]
    [InlineData("B", "A", true)]
    [InlineData("B", "A", false)]
    public void Validate_Should_Succeed_When_UnionMemberIsSplitAcrossSchemasSharingRootField(
        string imageSchemaName,
        string discountSchemaName,
        bool bannerFirst)
    {
        // arrange
        var members = bannerFirst ? "Banner | Coupon" : "Coupon | Banner";

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                imageSchemaName,
                $$"""
                type Query {
                    storefront: Storefront @shareable
                }

                type Storefront {
                    highlight: Highlight @shareable
                }

                union Highlight = {{members}}

                type Banner {
                    imageUrl: String
                }

                type Coupon {
                    code: String @shareable
                }
                """),
            new SourceSchemaText(
                discountSchemaName,
                $$"""
                type Query {
                    storefront: Storefront @shareable
                }

                type Storefront {
                    highlight: Highlight @shareable
                }

                union Highlight = {{members}}

                type Banner {
                    linkUrl: String
                }

                type Coupon {
                    code: String @shareable
                    discount: Int
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_Should_Fail_When_UnionMemberFieldIsOnlyInSchemaWithoutSharedPath(bool bannerFirst)
    {
        // arrange
        var sourceSchemas = CreateUnionMemberOutsideSharedPathSchemas(
            imageSchemaName: "A",
            linkSchemaName: "B",
            discountSchemaName: "C",
            bannerFirst);

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Coupon.discount' on path 'A:Query.storefront<Storefront> -> A:Storefront.highlight<Highlight>'.
              Unable to transition between schemas 'A' and 'C' for access to field 'C:Coupon.discount<Int>'.
                No lookups found for type 'Coupon' in schema 'C'.

            Unable to access the field 'Coupon.discount' on path 'A:Query.storefront<Storefront> -> B:Storefront.highlight<Highlight>'.
              Unable to transition between schemas 'B' and 'C' for access to field 'C:Coupon.discount<Int>'.
                No lookups found for type 'Coupon' in schema 'C'.
            """);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_Should_Fail_When_UnionMemberFieldIsOnlyInFirstSchemaWithoutSharedPath(bool bannerFirst)
    {
        // arrange
        var sourceSchemas = CreateUnionMemberOutsideSharedPathSchemas(
            imageSchemaName: "C",
            linkSchemaName: "B",
            discountSchemaName: "A",
            bannerFirst);

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Coupon.discount' on path 'B:Query.storefront<Storefront> -> B:Storefront.highlight<Highlight>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Coupon.discount<Int>'.
                No lookups found for type 'Coupon' in schema 'A'.

            Unable to access the field 'Coupon.discount' on path 'B:Query.storefront<Storefront> -> C:Storefront.highlight<Highlight>'.
              Unable to transition between schemas 'C' and 'A' for access to field 'A:Coupon.discount<Int>'.
                No lookups found for type 'Coupon' in schema 'A'.
            """);
    }

    [Theory]
    [InlineData("A", "C", true)]
    [InlineData("A", "C", false)]
    [InlineData("C", "A", true)]
    [InlineData("C", "A", false)]
    public void Validate_Should_Succeed_When_FieldIsReachedThroughSharedRootFieldAndLookup(
        string profileSchemaName,
        string accountsSchemaName,
        bool accountFirst)
    {
        // arrange
        var (firstViewerField, secondViewerField) = accountFirst
            ? ("account: Account", "displayName: String")
            : ("displayName: String", "account: Account");

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                profileSchemaName,
                $$"""
                type Query {
                    viewer: Viewer @shareable
                }

                type Viewer {
                    {{firstViewerField}}
                    {{secondViewerField}}
                }

                type Account {
                    id: ID! @shareable
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    viewer: Viewer @shareable
                }

                type Viewer {
                    locale: String
                }
                """),
            new SourceSchemaText(
                accountsSchemaName,
                """
                type Query {
                    accountById(id: ID!): Account @lookup @internal
                }

                type Account {
                    id: ID! @shareable
                    plan: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Validate_Should_Succeed_When_RequirementIsServedBySchemaSharingWholePath()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    wrapper: Wrapper @shareable
                }

                type Wrapper {
                    entity: Entity @shareable
                }

                type Entity {
                    id: ID @shareable
                    name(age: Int @require(field: "age")): String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    wrapper: Wrapper @shareable
                }

                type Wrapper {
                    entity: Entity @shareable
                }

                type Entity {
                    id: ID @shareable
                    age: Int
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Validate_Should_Fail_When_RequirementIsOnlyServedBySchemaWithoutRootFieldOfPath()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    wrapper: Wrapper
                    otherWrapper: Wrapper @shareable
                }

                type Wrapper {
                    entity: Entity @shareable
                }

                type Entity {
                    id: ID @shareable
                    name(age: Int @require(field: "entity.age")): String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    otherWrapper: Wrapper @shareable
                }

                type Wrapper {
                    entity: Entity @shareable
                }

                type Entity {
                    id: ID @shareable
                    age: Int @inaccessible
                    entity: Entity @inaccessible
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Entity.name' on path 'A:Query.wrapper<Wrapper> -> A:Wrapper.entity<Entity>'.
              Unable to satisfy the requirement '{ entity { age } }' on field 'A:Entity.name<String>'.
                Unable to satisfy the requirement 'entity { age }'.
                  Unable to access the required field 'Entity.entity' on path 'A:Wrapper.entity<Entity>'.
                    Unable to transition between schemas 'A' and 'B' for access to required field 'B:Entity.entity<Entity>'.
                      No lookups found for type 'Entity' in schema 'B'.
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_ExternalFieldOnSharedPathIsProvidedByRootField()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    topOrders: [Order] @shareable @provides(fields: "customer { id }")
                }

                type Order {
                    id: ID! @shareable
                    customer: Customer @external
                }

                type Customer {
                    id: ID! @external
                    name: String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    topOrders: [Order] @shareable
                }

                type Order {
                    id: ID! @shareable
                    customer: Customer
                }

                type Customer {
                    id: ID! @shareable
                    email: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Validate_Should_Fail_When_ExternalFieldOnSharedPathIsOnlyProvidedByOtherRootField()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    topOrders: [Order] @shareable
                    otherOrders: [Order] @provides(fields: "customer { id }")
                }

                type Order {
                    id: ID! @shareable
                    customer: Customer @external
                }

                type Customer {
                    id: ID! @external
                    name: String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    topOrders: [Order] @shareable
                    orderById(id: ID!): Order @lookup @internal
                }

                type Order {
                    id: ID! @shareable
                    customer: Customer
                }

                type Customer {
                    id: ID! @shareable
                    email: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Customer.name' on path 'A:Query.topOrders<Order> -> B:Order.customer<Customer>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Customer.name<String>'.
                No lookups found for type 'Customer' in schema 'A'.
            """
        ]);
    }

    private static SourceSchemaText[] CreateUnionMemberOutsideSharedPathSchemas(
        string imageSchemaName,
        string linkSchemaName,
        string discountSchemaName,
        bool bannerFirst)
    {
        var members = bannerFirst ? "Banner | Coupon" : "Coupon | Banner";

        return
        [
            new SourceSchemaText(
                imageSchemaName,
                $$"""
                type Query {
                    storefront: Storefront @shareable
                }

                type Storefront {
                    highlight: Highlight @shareable
                }

                union Highlight = {{members}}

                type Banner {
                    imageUrl: String
                }

                type Coupon {
                    code: String @shareable
                }
                """),
            new SourceSchemaText(
                linkSchemaName,
                $$"""
                type Query {
                    storefront: Storefront @shareable
                }

                type Storefront {
                    highlight: Highlight @shareable
                }

                union Highlight = {{members}}

                type Banner {
                    linkUrl: String
                }

                type Coupon {
                    code: String @shareable
                }
                """),
            new SourceSchemaText(
                discountSchemaName,
                """
                type Query {
                    coupons: [Coupon]
                }

                type Coupon {
                    code: String @shareable
                    discount: Int
                }
                """)
        ];
    }
}
