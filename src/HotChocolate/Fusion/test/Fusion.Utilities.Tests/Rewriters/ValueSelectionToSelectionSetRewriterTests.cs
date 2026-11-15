using HotChocolate.Fusion.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion.Rewriters;

public sealed class ValueSelectionToSelectionSetRewriterTests
{
    [Theory]
    [MemberData(nameof(ExamplesData))]
    public void Examples(string typeName, string selectedValue, string selectionSet)
    {
        // arrange
        var valueSelectionNode = new FieldSelectionMapParser(selectedValue).Parse();

        // act
        var selectionSetNode =
            s_valueSelectionToSelectionSetRewriter.Rewrite(
                valueSelectionNode,
                s_schema.Types[typeName]);

        // assert
        selectionSetNode.Print().MatchInlineSnapshot(selectionSet);
    }

    public static TheoryData<string, string, string> ExamplesData()
    {
        return new TheoryData<string, string, string>
        {
            {
                "Book",
                "id",
                """
                {
                    id
                }
                """
            },
            {
                "Book",
                "author.id",
                """
                {
                    author {
                        id
                    }
                }
                """
            },
            {
                "Query",
                "mediaById<Book>.isbn",
                """
                {
                    mediaById {
                        ... on Book {
                            isbn
                        }
                    }
                }
                """
            },
            {
                "Product",
                "dimensions.{ size, weight }",
                """
                {
                    dimensions {
                        size
                        weight
                    }
                }
                """
            },
            {
                "Product",
                "{ size: dimensions.size, weight: dimensions.weight }",
                """
                {
                    dimensions {
                        size
                        weight
                    }
                }
                """
            },
            {
                "Product",
                "parts[id]",
                """
                {
                    parts {
                        id
                    }
                }
                """
            },
            {
                "Product",
                "parts[{ id, name }]",
                """
                {
                    parts {
                        id
                        name
                    }
                }
                """
            },
            {
                "Product",
                "parts[[{ id, name }]]",
                """
                {
                    parts {
                        id
                        name
                    }
                }
                """
            },
            {
                "Product",
                """price(currency: "USD")""",
                """
                {
                    price(currency: "USD")
                }
                """
            },
            {
                "Product",
                """{ averagePrice: category.averagePrice(currency: "USD") }""",
                """
                {
                    category {
                        averagePrice(currency: "USD")
                    }
                }
                """
            },
            {
                "Location",
                "{ coordinates: coordinates[{ lat: x, lon: y }] }",
                """
                {
                    coordinates {
                        x
                        y
                    }
                }
                """
            },
            {
                "Query",
                "mediaById<Book>.title | mediaById<Movie>.movieTitle",
                """
                {
                    mediaById {
                        ... on Book {
                            title
                        }
                        ... on Movie {
                            movieTitle
                        }
                    }
                }
                """
            },
            {
                "Media",
                "{ bookId: <Book>.id } | { movieId: <Movie>.id }",
                """
                {
                    ... on Book {
                        id
                    }
                    ... on Movie {
                        id
                    }
                }
                """
            },
            {
                "Media",
                "{ nested: { bookId: <Book>.id } | { movieId: <Movie>.id } }",
                """
                {
                    ... on Book {
                        id
                    }
                    ... on Movie {
                        id
                    }
                }
                """
            },
            {
                "Example",
                "a | b | c",
                """
                {
                    a
                    b
                    c
                }
                """
            }
        };
    }

    [Fact]
    public void Rewrite_PathSegmentWithArgument_KeepsArgument()
    {
        // arrange
        var value = new FieldSelectionMapParser("dimension(unit: METRIC).length").Parse();

        // act
        var selectionSet = ValueSelectionToSelectionSetRewriter.Rewrite([value]);

        // assert
        Assert.Equal("{ dimension(unit: METRIC) { length } }", selectionSet.ToString(indented: false));
    }

    [Fact]
    public void Rewrite_ObjectFieldWithArgument_KeepsArgument()
    {
        // arrange
        var value = new FieldSelectionMapParser("{ width(unit: IMPERIAL) }").Parse();

        // act
        var selectionSet = ValueSelectionToSelectionSetRewriter.Rewrite([value]);

        // assert
        Assert.Contains("width(unit: IMPERIAL)", selectionSet.ToString(indented: false));
    }

    [Fact]
    public void Rewrite_Should_ReturnNoSelections_When_ValueIsContextPath()
    {
        // arrange
        var value = new FieldSelectionMapParser("$userContext.userCurrency.isoCode").Parse();

        // act
        var selectionSet = s_valueSelectionToSelectionSetRewriter.Rewrite(value, s_schema.Types["Order"]);

        // assert
        Assert.Empty(selectionSet.Selections);
    }

    [Fact]
    public void Rewrite_Should_ReturnNoSelections_When_AllValuesAreContextPaths()
    {
        // arrange
        var values = new IValueSelectionNode[]
        {
            new FieldSelectionMapParser("$userContext.userCurrency.isoCode").Parse(),
            new FieldSelectionMapParser("{ locale: $account<User>.locale }").Parse()
        };

        // act
        var selectionSet = s_valueSelectionToSelectionSetRewriter.Rewrite(values, s_schema.Types["Order"]);

        // assert
        Assert.Empty(selectionSet.Selections);
    }

    [Fact]
    public void Rewrite_Should_SkipContextField_When_ObjectMixesContextAndRegularPaths()
    {
        // arrange
        var value = new FieldSelectionMapParser(
            "{ currency: $userContext.userCurrency.isoCode, rate: exchange.rate }").Parse();

        // act
        var selectionSet = s_valueSelectionToSelectionSetRewriter.Rewrite(value, s_schema.Types["Order"]);

        // assert
        selectionSet.Print().MatchInlineSnapshot(
            """
            {
                exchange {
                    rate
                }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_SkipContextBranch_When_ChoiceMixesContextAndRegularPaths()
    {
        // arrange
        var value = new FieldSelectionMapParser("$userContext.userCurrency | id").Parse();

        // act
        var selectionSet = s_valueSelectionToSelectionSetRewriter.Rewrite(value, s_schema.Types["Order"]);

        // assert
        selectionSet.Print().MatchInlineSnapshot(
            """
            {
                id
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_SkipContextPaths_When_ListOrPathObjectWrapsThem()
    {
        // arrange
        var values = new IValueSelectionNode[]
        {
            new FieldSelectionMapParser("$userContext.items[{ id, name }]").Parse(),
            new FieldSelectionMapParser("$userContext.currency.{ isoCode }").Parse(),
            new FieldSelectionMapParser("exchange.{ rate }").Parse()
        };

        // act
        var selectionSet = s_valueSelectionToSelectionSetRewriter.Rewrite(values, s_schema.Types["Order"]);

        // assert
        selectionSet.Print().MatchInlineSnapshot(
            """
            {
                exchange {
                    rate
                }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_SkipContextPaths_When_UsedWithoutSchema()
    {
        // arrange
        var values = new IValueSelectionNode[]
        {
            new FieldSelectionMapParser("{ currency: $userContext.userCurrency.isoCode, rate: exchange.rate }").Parse(),
            new FieldSelectionMapParser("$userContext.userCurrency.isoCode").Parse()
        };

        // act
        var selectionSet = ValueSelectionToSelectionSetRewriter.Rewrite(values);

        // assert
        Assert.Equal("{ exchange { rate } }", selectionSet.ToString(indented: false));
    }

    [Fact]
    public void RewriteContextPaths_Should_ReturnSelectionSetPerContext_When_PathIsNested()
    {
        // arrange
        var value = new FieldSelectionMapParser("$userContext.userCurrency.isoCode").Parse();

        // act
        var contexts = ValueSelectionToSelectionSetRewriter.RewriteContextPaths([value]);

        // assert
        var context = Assert.Single(contexts);
        Assert.Equal("userContext", context.Key);
        context.Value.Print().MatchInlineSnapshot(
            """
            {
                userCurrency {
                    isoCode
                }
            }
            """);
    }

    [Fact]
    public void RewriteContextPaths_Should_IgnoreRegularPaths_When_ObjectMixesThem()
    {
        // arrange
        var value = new FieldSelectionMapParser(
            "{ currency: $userContext.userCurrency.isoCode, rate: exchange.rate }").Parse();

        // act
        var contexts = ValueSelectionToSelectionSetRewriter.RewriteContextPaths([value]);

        // assert
        var context = Assert.Single(contexts);
        Assert.Equal("userContext", context.Key);
        context.Value.Print().MatchInlineSnapshot(
            """
            {
                userCurrency {
                    isoCode
                }
            }
            """);
    }

    [Fact]
    public void RewriteContextPaths_Should_UseTypeConditions_When_ChoiceHasTypedContextPaths()
    {
        // arrange
        var value = new FieldSelectionMapParser(
            "$account<User>.locale | $account<Organization>.defaultLocale").Parse();

        // act
        var contexts = ValueSelectionToSelectionSetRewriter.RewriteContextPaths([value]);

        // assert
        var context = Assert.Single(contexts);
        Assert.Equal("account", context.Key);
        context.Value.Print().MatchInlineSnapshot(
            """
            {
                ... on User {
                    locale
                }
                ... on Organization {
                    defaultLocale
                }
            }
            """);
    }

    [Fact]
    public void RewriteContextPaths_Should_KeepArgumentsAndSegmentTypeConditions_When_PathHasThem()
    {
        // arrange
        var value = new FieldSelectionMapParser(
            "$account.profile<Admin>.settings(unit: METRIC).locale").Parse();

        // act
        var contexts = ValueSelectionToSelectionSetRewriter.RewriteContextPaths([value]);

        // assert
        var context = Assert.Single(contexts);
        Assert.Equal("account", context.Key);
        context.Value.Print().MatchInlineSnapshot(
            """
            {
                profile {
                    ... on Admin {
                        settings(unit: METRIC) {
                            locale
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public void RewriteContextPaths_Should_GroupByContextName_When_MultipleContextsAreUsed()
    {
        // arrange
        var values = new IValueSelectionNode[]
        {
            new FieldSelectionMapParser("$a.x.y").Parse(),
            new FieldSelectionMapParser("{ first: $b.z, second: $a.x.w, third: id }").Parse(),
            new FieldSelectionMapParser("$a.items[id]").Parse(),
            new FieldSelectionMapParser("$b.owner.name").Parse()
        };

        // act
        var contexts = ValueSelectionToSelectionSetRewriter.RewriteContextPaths(values);

        // assert
        Assert.Equal(["a", "b"], contexts.Keys.Order());
        contexts["a"].Print().MatchInlineSnapshot(
            """
            {
                x {
                    y
                }
                x {
                    w
                }
                items {
                    id
                }
            }
            """);
        contexts["b"].Print().MatchInlineSnapshot(
            """
            {
                z
                owner {
                    name
                }
            }
            """);
    }

    [Fact]
    public void RewriteContextPaths_Should_ReturnEmpty_When_NoContextPathIsUsed()
    {
        // arrange
        var value = new FieldSelectionMapParser("{ rate: exchange.rate }").Parse();

        // act
        var contexts = ValueSelectionToSelectionSetRewriter.RewriteContextPaths([value]);

        // assert
        Assert.Empty(contexts);
    }

    [Fact]
    public void RewriteContextPaths_Should_CollectBothContexts_When_AContextPathIsInTheObjectSelectionOfAnotherContextPath()
    {
        // arrange
        var value = new FieldSelectionMapParser(
            "$tenant.settings.{ currency: currency, locale: $user.locale }").Parse();

        // act
        var contexts = ValueSelectionToSelectionSetRewriter.RewriteContextPaths([value]);

        // assert
        Assert.Equal(["tenant", "user"], contexts.Keys.Order());
        contexts["tenant"].Print().MatchInlineSnapshot(
            """
            {
                settings {
                    currency
                }
            }
            """);
        contexts["user"].Print().MatchInlineSnapshot(
            """
            {
                locale
            }
            """);
    }

    [Fact]
    public void RewriteContextPaths_Should_CollectBothContexts_When_AContextPathIsInTheListSelectionOfAnotherContextPath()
    {
        // arrange
        var value = new FieldSelectionMapParser(
            "$tenant.regions[{ code: code, locale: $user.locale }]").Parse();

        // act
        var contexts = ValueSelectionToSelectionSetRewriter.RewriteContextPaths([value]);

        // assert
        Assert.Equal(["tenant", "user"], contexts.Keys.Order());
        contexts["tenant"].Print().MatchInlineSnapshot(
            """
            {
                regions {
                    code
                }
            }
            """);
        contexts["user"].Print().MatchInlineSnapshot(
            """
            {
                locale
            }
            """);
    }

    [Fact]
    public void RewriteContextPaths_Should_ReturnNoFragmentWithoutTypeCondition_When_ObjectSelectionsAreNested()
    {
        // arrange
        var value = new FieldSelectionMapParser(
            "$tenant.settings.{ currency: currency, region: region.{ code: code } }").Parse();

        // act
        var contexts = ValueSelectionToSelectionSetRewriter.RewriteContextPaths([value]);

        // assert
        var context = Assert.Single(contexts);
        Assert.Equal("tenant", context.Key);
        Assert.Equal("{ settings { currency region { code } } }", context.Value.ToString(indented: false));
    }

    [Fact]
    public void RewriteContextPaths_Should_CollectTheContext_When_TheContextPathIsInTheListSelectionOfAnEntityPath()
    {
        // arrange
        var value = new FieldSelectionMapParser(
            "items[{ weight: weight, currency: $tenant.currency }]").Parse();

        // act
        var contexts = ValueSelectionToSelectionSetRewriter.RewriteContextPaths([value]);

        // assert
        var context = Assert.Single(contexts);
        Assert.Equal("tenant", context.Key);
        context.Value.Print().MatchInlineSnapshot(
            """
            {
                currency
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_SelectTypeName_When_TheSelectionOfAPathReadsOnlyContexts()
    {
        // arrange
        var values = new IValueSelectionNode[]
        {
            new FieldSelectionMapParser("items[{ currency: $tenant.currency }]").Parse(),
            new FieldSelectionMapParser("dimension.{ currency: $tenant.currency }").Parse()
        };

        // act
        var selectionSet = s_valueSelectionToSelectionSetRewriter.Rewrite(values, s_schema.Types["Shipment"]);

        // assert
        selectionSet.Print().MatchInlineSnapshot(
            """
            {
                items {
                    __typename
                }
                dimension {
                    __typename
                }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_SelectTypeName_When_TheListSelectionIsAContextPath()
    {
        // arrange
        var value = new FieldSelectionMapParser("items[$tenant.currency]").Parse();

        // act
        var selectionSet = s_valueSelectionToSelectionSetRewriter.Rewrite(value, s_schema.Types["Shipment"]);

        // assert
        selectionSet.Print().MatchInlineSnapshot(
            """
            {
                items {
                    __typename
                }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_SelectTypeName_When_UsedWithoutSchemaAndTheSelectionReadsOnlyContexts()
    {
        // arrange
        var values = new IValueSelectionNode[]
        {
            new FieldSelectionMapParser("items[{ currency: $tenant.currency }]").Parse(),
            new FieldSelectionMapParser("dimension.{ currency: $tenant.currency }").Parse()
        };

        // act
        var selectionSet = ValueSelectionToSelectionSetRewriter.Rewrite(values);

        // assert
        Assert.Equal(
            "{ items { __typename } dimension { __typename } }",
            selectionSet.ToString(indented: false));
    }

    [Fact]
    public void RewriteContextPaths_Should_SelectTypeName_When_TheSelectionOfAContextPathReadsOnlyAnotherContext()
    {
        // arrange
        var value = new FieldSelectionMapParser("$tenant.settings.{ locale: $user.locale }").Parse();

        // act
        var contexts = ValueSelectionToSelectionSetRewriter.RewriteContextPaths([value]);

        // assert
        Assert.Equal(["tenant", "user"], contexts.Keys.Order());
        contexts["tenant"].Print().MatchInlineSnapshot(
            """
            {
                settings {
                    __typename
                }
            }
            """);
        contexts["user"].Print().MatchInlineSnapshot(
            """
            {
                locale
            }
            """);
    }

    private static readonly ISchemaDefinition s_schema = SchemaParser.Parse(
        """
        type Query {
            mediaById(mediaId: ID!): Media
        }

        interface Media {
            id: ID!
        }

        type Book implements Media {
            id: ID!
            title: String!
            isbn: String!
            author: Author!
        }

        type Movie implements Media {
            id: ID!
            movieTitle: String!
            releaseDate: String!
        }

        type Author {
            id: ID!
            books: [Book!]!
        }

        type Product {
            dimensions: Dimensions!
            parts: [Part!]!
            price(currency: String!): Int
            category: Category
        }

        type Category {
            averagePrice(currency: String!): Int
        }

        type Dimensions {
            size: Int!
            weight: Float!
        }

        type Part {
            id: ID!
            name: String!
        }

        type Location {
            coordinates: [Coordinate!]!
        }

        type Coordinate {
            x: Int!
            y: Int!
        }

        type Order {
            id: ID!
            exchange: Exchange!
        }

        type Shipment {
            items: [Part!]!
            dimension: Dimensions
        }

        type Exchange {
            rate: Float!
        }

        type Example {
            a: Int!
            b: Int!
            c: Int!
        }
        """);

    private static readonly ValueSelectionToSelectionSetRewriter
        s_valueSelectionToSelectionSetRewriter = new(s_schema);
}
