using CookieCrumble;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Shared;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class DirectiveTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Expose_TypeSystem_Directives_In_Fusion_Graph()
    {
        var schemaText = """
                         schema @test {
                           query: Query
                           mutation: Mutation
                           subscription: Subscription
                         }

                         type Query @test {
                           enumField: TestEnum
                           objectField(input: TestInput @test): TestOutput
                           scalarField: Test @test
                         }

                         type Mutation @test {
                           mutationField(input: TestInput @test): String @test
                         }

                         type Subscription @test {
                           subscriptionField(input: TestInput @test): String @test
                         }

                         type TestOutput implements TestInterface @test {
                           field: Int @test
                         }

                         interface TestInterface @test {
                           field: Int @test
                         }

                         union TestUnion @test = TestOutput

                         input TestInput @test {
                           inputField: Int @test
                         }

                         enum TestEnum @test {
                           ENUM_VALUE @test
                         }

                         scalar Test @test

                         "A test directive"
                         directive @test(arg: String = "test") repeatable on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
                         """;

        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString(schemaText)
                .AddType(new AnyType("Test"))
                .AddResolverMocking());

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(schemaText);
    }

    [Fact]
    public async Task Remove_Executable_Locations_From_Directive_Definition()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: String @test
            }

            "A test directive"
            directive @test on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION | QUERY | MUTATION | SUBSCRIPTION | FIELD | FRAGMENT_DEFINITION | FRAGMENT_SPREAD | INLINE_FRAGMENT | VARIABLE_DEFINITION
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: String @test
            }

            "A test directive"
            directive @test on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Ignore_Purely_Executable_Directive_Definitions()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: String
            }

            "A test directive"
            directive @test on QUERY | MUTATION | SUBSCRIPTION | FIELD | FRAGMENT_DEFINITION | FRAGMENT_SPREAD | INLINE_FRAGMENT | VARIABLE_DEFINITION
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: String
            }
            """);
    }

    [Fact]
    public async Task Merge_Directive_Definitions_With_Same_Arguments()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field1: String @test(arg: "test")
            }

            directive @test(arg: [String!]!) on FIELD_DEFINITION
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field2: String @test(arg: "test")
            }

            directive @test(arg: [String!]!) on FIELD_DEFINITION
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field1: String @test(arg: "test")
              field2: String @test(arg: "test")
            }

            directive @test(arg: [String!]!) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Throw_On_Directive_Definition_Argument_Name_Mismatch()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field1: String @test(arg1: "test")
            }

            directive @test(arg1: String) on FIELD_DEFINITION
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field2: String @test(arg2: "test")
            }

            directive @test(arg2: String) on FIELD_DEFINITION
            """);

        var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var act = () => subgraphs.GetFusionGraphAsync();

        // assert
        await Assert.ThrowsAsync<CompositionException>(act);
    }

    [Fact]
    public async Task Throw_On_Directive_Argument_Type_Mismatch()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field1: String @test(arg: 1)
            }

            directive @test(arg: Int) on FIELD_DEFINITION
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field2: String @test(arg: "test")
            }

            directive @test(arg: String) on FIELD_DEFINITION
            """);

        var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var act = () => subgraphs.GetFusionGraphAsync();

        // assert
        await Assert.ThrowsAsync<CompositionException>(act);
    }

    [Fact]
    public async Task Merge_Non_Repeatable_Directive_On_Same_Field()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              foo: SubType @test
            }

            type SubType {
              bar: String @test
            }

            directive @test on FIELD_DEFINITION
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              foo: SubType @test
            }

            type SubType {
              bar: String @test
            }

            directive @test on FIELD_DEFINITION
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              foo: SubType @test
            }

            type SubType {
              bar: String @test
            }

            directive @test on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Repeat_Repeatable_Directive_On_Same_Field()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              foo: SubType @test(arg: "A")
            }

            type SubType {
              bar: String @test(arg: "A")
            }

            directive @test(arg: String) repeatable on FIELD_DEFINITION
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              foo: SubType @test(arg: "B")
            }

            type SubType {
              bar: String @test(arg: "B")
            }

            directive @test(arg: String) repeatable on FIELD_DEFINITION
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              foo: SubType @test(arg: "A") @test(arg: "B")
            }

            type SubType {
              bar: String @test(arg: "A") @test(arg: "B")
            }

            directive @test(arg: String) repeatable on FIELD_DEFINITION
            """);
    }

    private static DocumentNode GetSchemaWithoutFusion(SchemaDefinition fusionGraph)
    {
        var fusionGraphDoc = Utf8GraphQLParser.Parse(SchemaFormatter.FormatAsString(fusionGraph));
        var typeNames = FusionTypeNames.From(fusionGraphDoc);
        var rewriter = new FusionGraphConfigurationToSchemaRewriter();

        return (DocumentNode)rewriter.Rewrite(fusionGraphDoc, new(typeNames))!;
    }
}
