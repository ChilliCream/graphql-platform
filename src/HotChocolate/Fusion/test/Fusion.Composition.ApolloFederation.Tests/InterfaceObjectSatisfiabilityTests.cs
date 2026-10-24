using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class InterfaceObjectSatisfiabilityTests
{
    [Theory]
    [InlineData("A", "B")]
    [InlineData("B", "A")]
    public void Compose_Should_Succeed_When_InterfaceObjectStandsInForEntityInterface(
        string interfaceSchemaName,
        string standInSchemaName)
    {
        // arrange
        const string standIn =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@interfaceObject"])

            type Query {
                trending: [Media!]!
            }

            type Media @key(fields: "id") @interfaceObject {
                id: ID!
                views: Int!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText(interfaceSchemaName, MediaSchema),
            new SourceSchemaText(standInSchemaName, standIn));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_InterfaceObjectContributesNoFields()
    {
        // arrange
        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@interfaceObject"])

            type Query {
                latest: Media
            }

            type Media @key(fields: "id") @interfaceObject {
                id: ID!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("A", MediaSchema),
            new SourceSchemaText("B", b));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_AddStandInFieldToEveryImplementation_When_ImplementationsComeFromSeveralSchemas()
    {
        // arrange
        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@interfaceObject"])

            type Media @key(fields: "id") @interfaceObject {
                id: ID!
                views: Int!
            }
            """;

        const string c =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                podcasts: [Podcast!]!
            }

            interface Media @key(fields: "id") {
                id: ID!
                title: String!
            }

            type Podcast implements Media @key(fields: "id") {
                id: ID!
                title: String!
                host: String!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("A", MediaSchema),
            new SourceSchemaText("B", b),
            new SourceSchemaText("C", c));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        string.Join(
            "\n",
            result.Value.Types
                .OfType<MutableObjectTypeDefinition>()
                .Where(type => type.IsImplementing("Media"))
                .OrderBy(type => type.Name, StringComparer.Ordinal)
                .Select(type => $"{type.Name}.{type.Fields["views"]}"))
            .MatchInlineSnapshot(
                """
                Book.views: Int! @fusion__field(schema: B)
                Movie.views: Int! @fusion__field(schema: B)
                Podcast.views: Int! @fusion__field(schema: B)
                """);
    }

    [Fact]
    public void Compose_Should_Succeed_When_SeveralInterfaceObjectsStandInForOneInterface()
    {
        // arrange
        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@interfaceObject"])

            type Query {
                trending: [Media!]!
            }

            type Media @key(fields: "id") @interfaceObject {
                id: ID!
                views: Int!
            }
            """;

        const string c =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@interfaceObject"])

            type Query {
                topRated: [Media!]!
            }

            type Media @key(fields: "id") @interfaceObject {
                id: ID!
                rating: Float!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("A", MediaSchema),
            new SourceSchemaText("B", b),
            new SourceSchemaText("C", c));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_InterfaceObjectImplementsAnotherEntityInterface()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                items: [Item!]!
                media: [Media!]!
            }

            interface Item @key(fields: "id") {
                id: ID!
            }

            interface Media implements Item @key(fields: "id") {
                id: ID!
                title: String!
            }

            type Book implements Media & Item @key(fields: "id") {
                id: ID!
                title: String!
            }

            type Movie implements Media & Item @key(fields: "id") {
                id: ID!
                title: String!
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@interfaceObject"])

            type Query {
                trending: [Media!]!
            }

            interface Item {
                id: ID!
            }

            type Media implements Item @key(fields: "id") @interfaceObject {
                id: ID!
                views: Int!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("A", a),
            new SourceSchemaText("B", b));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_InterfaceObjectFieldsReturnTheInterfaceRecursively()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                media: [Media!]!
            }

            interface Media @key(fields: "id") {
                id: ID!
                title: String!
                related: [Media!]!
                sequel: Media
                prequel: Media
            }

            type Book implements Media @key(fields: "id") {
                id: ID!
                title: String!
                related: [Media!]!
                sequel: Media
                prequel: Media
            }

            type Movie implements Media @key(fields: "id") {
                id: ID!
                title: String!
                related: [Media!]!
                sequel: Media
                prequel: Media
            }

            type Song implements Media @key(fields: "id") {
                id: ID!
                title: String!
                related: [Media!]!
                sequel: Media
                prequel: Media
            }

            type Show implements Media @key(fields: "id") {
                id: ID!
                title: String!
                related: [Media!]!
                sequel: Media
                prequel: Media
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@interfaceObject"])

            type Query {
                trending: [Media!]!
            }

            type Media @key(fields: "id") @interfaceObject {
                id: ID!
                views: Int!
                similar: [Media!]!
                recommended: [Media!]!
                next: Media
            }
            """;

        const string c =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@interfaceObject"])

            type Query {
                featured: Media
            }

            type Media @key(fields: "id") @interfaceObject {
                id: ID!
                rating: Float!
                remakes: [Media!]!
                inspiredBy: Media
                spinOff: Media
            }
            """;

        const string d =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@interfaceObject"])

            type Query {
                newest: [Media!]!
            }

            type Media @key(fields: "id") @interfaceObject {
                id: ID!
                likes: Int!
                fans: [Fan!]!
            }

            type Fan {
                favorite: Media
                watched: [Media!]!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("A", a),
            new SourceSchemaText("B", b),
            new SourceSchemaText("C", c),
            new SourceSchemaText("D", d));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_InterfaceObjectInterfaceHasInaccessibleImplementor()
    {
        // arrange
        const string catalog =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@inaccessible"])

            interface Item @key(fields: "id") {
                id: ID!
                common: String
            }

            type Visible implements Item @key(fields: "id") {
                id: ID!
                common: String
            }

            type Private implements Item @key(fields: "id") @inaccessible {
                id: ID!
                common: String
            }

            type Query {
                items: [Item]
                alive: String
            }
            """;

        const string extra =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@interfaceObject"])

            type Item @key(fields: "id") @interfaceObject {
                id: ID!
                extra: String
            }

            type Query {
                extraItems: [Item]
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("catalog", catalog),
            new SourceSchemaText("extra", extra));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_OnlyOtherImplementorOfInterfaceObjectInterfaceIsInaccessible()
    {
        // arrange
        const string accounts =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@inaccessible"])

            interface Account @key(fields: "id") {
                id: ID!
                name: String
            }

            type Personal implements Account @key(fields: "id") {
                id: ID!
                name: String
            }

            type Internal implements Account @key(fields: "id") @inaccessible {
                id: ID!
                name: String
            }
            """;

        const string ledger =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@interfaceObject"])

            type Query {
                rows: [Row]
            }

            type Row {
                account: Account
            }

            type Account @key(fields: "id") @interfaceObject {
                id: ID!
                balance: Int
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("accounts", accounts),
            new SourceSchemaText("ledger", ledger));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Fail_When_InterfaceHasNoKeyForInterfaceObject()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                media: [Media!]!
            }

            interface Media {
                id: ID!
                title: String!
            }

            type Book implements Media @key(fields: "id") {
                id: ID!
                title: String!
            }

            type Movie implements Media @key(fields: "id") {
                id: ID!
                title: String!
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@interfaceObject"])

            type Query {
                trending: [Media!]!
            }

            type Media @key(fields: "id") @interfaceObject {
                id: ID!
                views: Int!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("A", a),
            new SourceSchemaText("B", b));

        // assert
        Assert.True(result.IsFailure);
        log.Select(entry => entry.ToString()).MatchInlineSnapshots(
            [
                """
                {
                  "message": "The @interfaceObject stand-in 'Media' in schema 'B' declares the key 'id', which does not match any key declared on the interface it stands in for.",
                  "code": "INTERFACE_OBJECT_KEY_MISMATCH",
                  "severity": "Error",
                  "coordinate": "Media",
                  "member": "Media",
                  "schema": "B",
                  "extensions": {}
                }
                """
            ]);
    }

    private const string MediaSchema =
        """
        extend schema
          @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

        type Query {
            media: [Media!]!
        }

        interface Media @key(fields: "id") {
            id: ID!
            title: String!
        }

        type Book implements Media @key(fields: "id") {
            id: ID!
            title: String!
            isbn: String!
        }

        type Movie implements Media @key(fields: "id") {
            id: ID!
            title: String!
            runtime: Int!
        }
        """;

    private static (CompositionResult<MutableSchemaDefinition> Result, CompositionLog Log) Compose(
        params SourceSchemaText[] sourceSchemas)
    {
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            sourceSchemas,
            new SchemaComposerOptions
            {
                Satisfiability = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true }
            },
            log);

        return (composer.Compose(), log);
    }
}
