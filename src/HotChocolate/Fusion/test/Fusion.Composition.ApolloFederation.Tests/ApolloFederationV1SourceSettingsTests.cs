using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class ApolloFederationV1SourceSettingsTests
{
    [Fact]
    public void Compose_Should_TransformRawFederationV1_When_ExplicitlyConfigured()
    {
        // act
        var (result, log) = Compose(
            [new SourceSchemaText("Products", FederationV1Schema)],
            ["Products"]);

        // assert
        Assert.True(result.IsSuccess, FormatFailure(result, log));
        Assert.Equal(
            "ApolloFederation",
            GetConnectorKind(result.Value, "PRODUCTS"));
        var document = result.Value.ToSyntaxNode();
        var transformedSchema = new DocumentNode(
            document.Definitions.Where(
                definition => definition is SchemaDefinitionNode
                    || definition is INamedSyntaxNode named
                        && (!named.Name.Value.StartsWith("fusion__", StringComparison.Ordinal)
                            || named.Name.Value == "fusion__Schema"))
                .ToArray());

        transformedSchema.ToString().MatchInlineSnapshot(
            """
            schema
              @fusion__execution(
                nodeResolution: GATEWAY
                shareableFieldRuntimeTypeRouting: SOURCE_LOCAL
              ) {
              query: Query
            }

            type Query @fusion__type(schema: PRODUCTS) {
              product: ProductV1 @fusion__field(schema: PRODUCTS)
            }

            type ProductV1
              @fusion__type(schema: PRODUCTS)
              @fusion__lookup(
                schema: PRODUCTS
                key: "id"
                field: "fusion__lookup_productV1ById(id: ID!): ProductV1"
                map: ["id"]
                path: null
                internal: true
              ) {
              displayName: String @fusion__field(schema: PRODUCTS)
              id: ID! @fusion__field(schema: PRODUCTS, sourceExternal: true)
            }

            "The fusion__Schema enum is a generated type used within an execution schema document to refer to a source schema in a type-safe manner."
            enum fusion__Schema {
              PRODUCTS @fusion__schema_metadata(name: "Products", kind: "ApolloFederation")
            }
            """);
    }

    [Fact]
    public void Compose_Should_RejectRawFederationV1_When_NotConfigured()
    {
        // act
        var (result, _) = Compose(
            [new SourceSchemaText("Products", FederationV1Schema)],
            []);

        // assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Compose_Should_RejectV2KeyArgument_When_ExplicitV1IsConfigured()
    {
        // arrange
        var schema = FederationV1Schema.Replace(
            "@key(fields: \"id\")",
            "@key(fields: \"id\", resolvable: false)",
            StringComparison.Ordinal);

        // act
        var (result, _) = Compose(
            [new SourceSchemaText("Products", schema)],
            ["Products"]);

        // assert
        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(
        "id: ID! @external",
        "id: ID! @external(reason: \"legacy\")")]
    [InlineData(
        "type ProductV1 @key(fields: \"id\") @extends",
        "type ProductV1 @key(fields: \"id\") @extends @external")]
    public void Compose_Should_RejectV2ExternalApplication_When_ExplicitV1IsConfigured(
        string current,
        string replacement)
    {
        // arrange
        var schema = FederationV1Schema.Replace(
            current,
            replacement,
            StringComparison.Ordinal);

        // act
        var (result, _) = Compose(
            [new SourceSchemaText("Products", schema)],
            ["Products"]);

        // assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Parse_Should_RegisterRawProvides_When_ExplicitV1IsConfigured()
    {
        // arrange
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(
            new SourceSchemaText(
                "Products",
                """
                type Query {
                  product: Product @provides(fields: "sku")
                }

                type Product {
                  sku: String @external
                }
                """),
            log,
            isApolloFederationV1: true);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsSuccess, FormatFailure(result, log));
        var query = Assert.IsType<MutableObjectTypeDefinition>(result.Value.QueryType);
        Assert.True(query.Fields["product"].Directives.ContainsName("provides"));
    }

    [Fact]
    public void Compose_Should_RejectInvalidTagArguments_When_ExplicitV1IsConfigured()
    {
        // arrange
        var schema = (FederationV1TagDefinition + FederationV1Schema).Replace(
            "displayName: String",
            "displayName: String @tag(name: \"public\", unexpected: \"value\")",
            StringComparison.Ordinal);

        // act
        var (result, _) = Compose(
            [new SourceSchemaText("Products", schema)],
            ["Products"]);

        // assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Compose_Should_RejectTagOnSchema_When_ExplicitV1IsConfigured()
    {
        // arrange
        const string schema =
            """
            extend schema @tag(name: "public")

            """ + FederationV1TagDefinition + FederationV1Schema;

        // act
        var (result, _) = Compose(
            [new SourceSchemaText("Products", schema)],
            ["Products"]);

        // assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Compose_Should_RejectExplicitV1_When_FederationLinkIsPresent()
    {
        // act
        var (result, log) = Compose(
            [new SourceSchemaText("Products", FederationV2Schema)],
            ["Products"]);

        // assert
        Assert.True(result.IsFailure);
        log.Select(entry => entry.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "Source schema 'Products' cannot enable Apollo Federation v1 support because it links to the Apollo Federation specification.",
                "code": "CONFLICTING_APOLLO_FEDERATION_VERSION",
                "severity": "Error",
                "schema": "Products",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_AllowExplicitV1_When_UnrelatedLinkIsPresent()
    {
        // arrange
        const string schema =
            """
            extend schema @link(url: "https://specs.example.com/custom/v1.0")

            directive @link(url: String!) repeatable on SCHEMA

            """ + FederationV1Schema;

        // act
        var (result, log) = Compose(
            [new SourceSchemaText("Products", schema)],
            ["Products"]);

        // assert
        Assert.True(result.IsSuccess, FormatFailure(result, log));
    }

    [Fact]
    public void Compose_Should_SupportMixedFederationVersions_When_OnlyV1SourceIsConfigured()
    {
        // act
        var (result, log) = Compose(
            [
                new SourceSchemaText("Products", FederationV1Schema),
                new SourceSchemaText("Reviews", FederationV2Schema)
            ],
            ["Products"]);

        // assert
        Assert.True(result.IsSuccess, FormatFailure(result, log));
        Assert.Equal("ApolloFederation", GetConnectorKind(result.Value, "PRODUCTS"));
        Assert.Equal("ApolloFederation", GetConnectorKind(result.Value, "REVIEWS"));
    }

    [Fact]
    public void Compose_Should_LeaveFederationV2BehaviorUnchanged_When_V1IsNotConfigured()
    {
        // act
        var (result, log) = Compose(
            [new SourceSchemaText("Reviews", FederationV2Schema)],
            []);

        // assert
        Assert.True(result.IsSuccess, FormatFailure(result, log));
        Assert.Equal("ApolloFederation", GetConnectorKind(result.Value, "REVIEWS"));
    }

    private static (
        CompositionResult<MutableSchemaDefinition> Result,
        CompositionLog Log) Compose(
        SourceSchemaText[] sourceSchemas,
        string[] federationV1SourceNames)
    {
        var log = new CompositionLog();
        var options = new SchemaComposerOptions();

        foreach (var sourceName in federationV1SourceNames)
        {
            options.SourceSchemas[sourceName] = new SourceSchemaOptions
            {
                IsApolloFederationV1 = true
            };
        }

        var result = new SchemaComposer(sourceSchemas, options, log).Compose();
        return (result, log);
    }

    private static string? FormatFailure(
        CompositionResult<MutableSchemaDefinition> result,
        CompositionLog log)
        => result.IsSuccess
            ? null
            : string.Join(
                Environment.NewLine,
                result.Errors.Select(error => error.Message).Concat(log.Select(entry => entry.Message)));

    private static string GetConnectorKind(
        MutableSchemaDefinition mergedSchema,
        string valueName)
    {
        var schemaEnum = Assert.IsAssignableFrom<MutableEnumTypeDefinition>(
            mergedSchema.Types["fusion__Schema"]);
        Assert.True(schemaEnum.Values.TryGetValue(valueName, out var enumValue));
        var directive = Assert.IsType<Directive>(
            enumValue.Directives.FirstOrDefault("fusion__schema_metadata"));
        Assert.True(directive.Arguments.TryGetValue("kind", out var kindValue));
        return Assert.IsType<StringValueNode>(kindValue).Value;
    }

    private const string FederationV1Schema =
        """
        scalar _Any
        scalar _FieldSet

        type _Service {
          sdl: String
        }

        union _Entity = ProductV1

        type Query {
          product: ProductV1
          _entities(representations: [_Any!]!): [_Entity]!
          _service: _Service!
        }

        type ProductV1 @key(fields: "id") @extends {
          id: ID! @external
          displayName: String
        }
        """;

    private const string FederationV1TagDefinition =
        """
        directive @tag(name: String!) repeatable on
          | FIELD_DEFINITION
          | OBJECT
          | INTERFACE
          | UNION
          | ARGUMENT_DEFINITION
          | SCALAR
          | ENUM
          | ENUM_VALUE
          | INPUT_OBJECT
          | INPUT_FIELD_DEFINITION

        """;

    private const string FederationV2Schema =
        """
        extend schema
          @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

        type Query {
          review: Review
        }

        type Review @key(fields: "id") {
          id: ID!
        }
        """;
}
