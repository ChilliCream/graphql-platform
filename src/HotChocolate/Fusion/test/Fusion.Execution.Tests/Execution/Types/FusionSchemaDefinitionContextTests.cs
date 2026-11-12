using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionSchemaDefinitionContextTests
{
    [Fact]
    public void SetsContext_Should_ReturnTrueOnlyForTheTypeThatDeclaresIt_When_ContextIsOnObjectType()
    {
        // arrange
        var schema = CreateSchema();
        var user = schema.Types.GetType<FusionObjectTypeDefinition>("User");
        var transaction = schema.Types.GetType<FusionObjectTypeDefinition>("Transaction");

        // act
        var userSetsContext = schema.SetsContext(user, "A", "userContext");
        var transactionSetsContext = schema.SetsContext(transaction, "A", "userContext");

        // assert
        Assert.True(userSetsContext);
        Assert.False(transactionSetsContext);
    }

    [Fact]
    public void SetsContext_Should_ReturnFalse_When_SchemaOrContextNameDiffers()
    {
        // arrange
        var schema = CreateSchema();
        var user = schema.Types.GetType<FusionObjectTypeDefinition>("User");

        // act
        var otherContext = schema.SetsContext(user, "A", "otherContext");
        var otherSchema = schema.SetsContext(user, "B", "userContext");

        // assert
        Assert.False(otherContext);
        Assert.False(otherSchema);
    }

    [Fact]
    public void SetsContext_Should_ReturnTrue_When_ImplementedInterfaceDeclaresContext()
    {
        // arrange
        var schema = CreateSchema();
        var organization = schema.Types.GetType<FusionObjectTypeDefinition>("Organization");

        // act
        var inSchemaA = schema.SetsContext(organization, "A", "accountContext");
        var inSchemaB = schema.SetsContext(organization, "B", "accountContext");

        // assert
        Assert.True(inSchemaA);
        Assert.False(inSchemaB);
    }

    [Fact]
    public void SetsContext_Should_ReturnTrue_When_UnionMemberOfDeclaresContext()
    {
        // arrange
        var schema = CreateSchema();
        var book = schema.Types.GetType<FusionObjectTypeDefinition>("Book");
        var user = schema.Types.GetType<FusionObjectTypeDefinition>("User");

        // act
        var bookSetsContext = schema.SetsContext(book, "A", "mediaContext");
        var userSetsContext = schema.SetsContext(user, "A", "mediaContext");

        // assert
        Assert.True(bookSetsContext);
        Assert.False(userSetsContext);
    }

    [Fact]
    public void Contexts_Should_ExposeDeclaredContextNames_When_TypesDeclareContexts()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var user = schema.Types.GetType<FusionObjectTypeDefinition>("User");
        var accountable = schema.Types.GetType<FusionInterfaceTypeDefinition>("Accountable");
        var media = schema.Types.GetType<FusionUnionTypeDefinition>("Media");

        // assert
        Assert.Equal(["userContext"], user.Sources["A"].Contexts);
        Assert.Equal(["accountContext"], accountable.Sources["A"].Contexts);
        Assert.Equal(["mediaContext"], media.Sources["A"].Contexts);
    }

    [Fact]
    public void Create_Should_LoadRequirementsOfContextPathOnly_When_RequirementsArgumentIsAbsent()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var requirements = GetRequirements(schema, "Transaction", "amount");

        // assert
        Assert.Null(requirements.Requirements);
        Assert.Equal(["userContext"], requirements.ContextRequirements.Keys);
        requirements.ContextRequirements["userContext"].ToString().MatchInlineSnapshot(
            """
            {
              userCurrency {
                isoCode
              }
            }
            """);
    }

    [Fact]
    public void Create_Should_SplitRequirementsByRoot_When_MapMixesContextAndEntityPaths()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var requirements = GetRequirements(schema, "Transaction", "convertedAmount");

        // assert
        requirements.Requirements!.ToString().MatchInlineSnapshot(
            """
            {
              exchange {
                rate
              }
            }
            """);
        Assert.Equal(["userContext"], requirements.ContextRequirements.Keys);
        requirements.ContextRequirements["userContext"].ToString().MatchInlineSnapshot(
            """
            {
              userCurrency {
                isoCode
              }
            }
            """);
    }

    [Fact]
    public void Create_Should_HaveNoContextRequirements_When_MapHasNoContextPath()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var requirements = GetRequirements(schema, "Transaction", "fee");

        // assert
        Assert.Empty(requirements.ContextRequirements);
        requirements.Requirements!.ToString().MatchInlineSnapshot(
            """
            {
              exchange {
                rate
              }
            }
            """);
    }

    [Fact]
    public void Create_Should_KeepTypeConditions_When_ContextPathHasTypeName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var requirements = GetRequirements(schema, "Transaction", "label");

        // assert
        Assert.Null(requirements.Requirements);
        requirements.ContextRequirements["accountContext"].ToString().MatchInlineSnapshot(
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

    private static FieldRequirements GetRequirements(
        FusionSchemaDefinition schema,
        string typeName,
        string fieldName)
    {
        var type = schema.Types.GetType<FusionObjectTypeDefinition>(typeName);
        return type.Fields[fieldName].Sources["A"].Requirements!;
    }

    private static FusionSchemaDefinition CreateSchema()
        => FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse(
                """
                type Query @fusion__type(schema: A) {
                  me: User @fusion__field(schema: A)
                  media: Media @fusion__field(schema: A)
                }

                type User implements Accountable
                  @fusion__type(schema: A)
                  @fusion__implements(schema: A, interface: "Accountable")
                  @fusion__context(schema: A, name: "userContext") {
                  id: ID!
                  locale: String @fusion__field(schema: A)
                  userCurrency: Currency @fusion__field(schema: A)
                  transactions: [Transaction!]! @fusion__field(schema: A)
                }

                type Organization implements Accountable
                  @fusion__type(schema: A)
                  @fusion__type(schema: B)
                  @fusion__implements(schema: A, interface: "Accountable") {
                  id: ID!
                  defaultLocale: String @fusion__field(schema: A)
                }

                interface Accountable
                  @fusion__type(schema: A)
                  @fusion__context(schema: A, name: "accountContext") {
                  id: ID!
                }

                type Book @fusion__type(schema: A) {
                  id: ID!
                }

                union Media
                  @fusion__type(schema: A)
                  @fusion__unionMember(schema: A, member: "Book")
                  @fusion__context(schema: A, name: "mediaContext") = Book

                type Currency @fusion__type(schema: A) {
                  isoCode: String! @fusion__field(schema: A)
                }

                type Exchange @fusion__type(schema: A) {
                  rate: Float! @fusion__field(schema: A)
                }

                type Transaction @fusion__type(schema: A) {
                  id: ID!
                  exchange: Exchange @fusion__field(schema: A)
                  amount(currency: String!): Float
                    @fusion__field(schema: A)
                    @fusion__requires(
                      schema: A
                      field: "amount(currency: String!): Float"
                      map: ["$userContext.userCurrency.isoCode"]
                    )
                  convertedAmount(currency: String!, rate: Float!): Float
                    @fusion__field(schema: A)
                    @fusion__requires(
                      schema: A
                      requirements: "exchange { rate }"
                      field: "convertedAmount(currency: String!, rate: Float!): Float"
                      map: ["$userContext.userCurrency.isoCode", "exchange.rate"]
                    )
                  fee(rate: Float!): Float
                    @fusion__field(schema: A)
                    @fusion__requires(
                      schema: A
                      requirements: "exchange { rate }"
                      field: "fee(rate: Float!): Float"
                      map: ["exchange.rate"]
                    )
                  label(locale: String!): String
                    @fusion__field(schema: A)
                    @fusion__requires(
                      schema: A
                      field: "label(locale: String!): String"
                      map: ["$accountContext<User>.locale | $accountContext<Organization>.defaultLocale"]
                    )
                }

                enum fusion__Schema {
                  A
                  B
                }
                """));
}
