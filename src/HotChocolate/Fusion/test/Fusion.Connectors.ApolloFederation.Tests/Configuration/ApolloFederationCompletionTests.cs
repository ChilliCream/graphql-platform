using System.Reflection;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Configuration;

public sealed class ApolloFederationCompletionTests
{
    [Fact]
    public void Complete_Should_ProjectLookups_From_SchemaMetadata()
    {
        // arrange
        var schema = ComposeSchema(
            "products",
            """
            schema @fusion__connector(kind: "Apollo") {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
            }
            """);

        var configuration = new ApolloFederationSourceSchemaClientConfiguration(
            "products",
            "products",
            new Uri("http://products/graphql"));

        // act
        InvokeComplete(configuration, schema);

        // assert
        Assert.True(configuration.Lookups.TryGetValue("productById", out var lookup));
        Assert.Equal("Product", lookup.EntityTypeName);
        Assert.Equal("id", lookup.ArgumentToKeyFieldMap["id"]);
    }

    [Fact]
    public void Complete_Should_See_PopulatedLookupFieldType_When_OrderingHolds()
    {
        // arrange
        // Lookup.FieldType is populated inside an INeedsCompletion.Complete callback
        // that fires before the federation projection runs. If a future change to
        // CompositeSchemaBuilderContext re-orders completions (e.g. switches Add to
        // Insert(0, ...)), this projection would observe a null FieldType and crash,
        // so this test guards the ordering invariant.
        var schema = ComposeSchema(
            "products",
            """
            schema @fusion__connector(kind: "Apollo") {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var configuration = new ApolloFederationSourceSchemaClientConfiguration(
            "products",
            "products",
            new Uri("http://products/graphql"));

        // act
        InvokeComplete(configuration, schema);

        // assert
        var lookup = configuration.Lookups["productById"];
        Assert.False(string.IsNullOrEmpty(lookup.EntityTypeName));
    }

    [Fact]
    public void Complete_Should_BindRewriter_To_ProvidedSchema_For_HotReload()
    {
        // arrange
        // Schema rebuild must produce a fresh rewriter bound to the new schema.
        // The factory is stateless w.r.t. schema; the rewriter lives on the
        // configuration. Building two configurations from two FusionSchemaDefinitions
        // therefore produces two distinct rewriters.
        var schemaA = ComposeSchema(
            "products",
            """
            schema @fusion__connector(kind: "Apollo") {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var schemaB = ComposeSchema(
            "products",
            """
            schema @fusion__connector(kind: "Apollo") {
              query: Query
            }

            type Query {
              productByCode(code: String! @is(field: "code")): Product @lookup
            }

            type Product @key(fields: "code") {
              code: String!
            }
            """);

        var configurationA = new ApolloFederationSourceSchemaClientConfiguration(
            "products",
            "products",
            new Uri("http://products/graphql"));

        var configurationB = new ApolloFederationSourceSchemaClientConfiguration(
            "products",
            "products",
            new Uri("http://products/graphql"));

        // act
        InvokeComplete(configurationA, schemaA);
        InvokeComplete(configurationB, schemaB);

        // assert
        Assert.NotSame(configurationA.QueryRewriter, configurationB.QueryRewriter);
        Assert.True(configurationA.Lookups.ContainsKey("productById"));
        Assert.False(configurationA.Lookups.ContainsKey("productByCode"));
        Assert.True(configurationB.Lookups.ContainsKey("productByCode"));
        Assert.False(configurationB.Lookups.ContainsKey("productById"));
    }

    private static FusionSchemaDefinition ComposeSchema(string name, string sourceSdl)
    {
        var sources = new[] { new SourceSchemaText(name, sourceSdl) };
        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions();
        var composer = new SchemaComposer(sources, composerOptions, compositionLog);

        var result = composer.Compose();
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }

    private static void InvokeComplete(
        ApolloFederationSourceSchemaClientConfiguration configuration,
        FusionSchemaDefinition schema)
    {
        // INeedsCompletion is internal in HotChocolate.Fusion.Execution.Types.
        // The connector test project does not have IVT into that assembly, so we
        // invoke the explicit interface implementation through reflection. This
        // is a unit-level guard for the projection logic; the same code path is
        // exercised end-to-end by the compliance suite.
        var method = configuration
            .GetType()
            .GetMethod(
                "HotChocolate.Fusion.Types.Completion.INeedsCompletion.Complete",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException(
                "Could not locate INeedsCompletion.Complete on configuration.");

        method.Invoke(configuration, [schema, null]);
    }
}
