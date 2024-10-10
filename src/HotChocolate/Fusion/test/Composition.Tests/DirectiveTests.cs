using CookieCrumble;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Shared;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class DirectiveTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Expose_Static_Directives_In_Fusion_Graph()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString("""
                                       schema @test {
                                         query: Query
                                         mutation: Mutation
                                         subscription: Subscription
                                       }

                                       type Query @test {
                                         scalarField: Test @test
                                         enumField: TestEnum
                                         objectField(input: TestInput @test): TestOutput
                                       }

                                       type Mutation {
                                         mutationField: String
                                       }

                                       type Subscription {
                                         subscriptionField: String
                                       }

                                       input TestInput @test {
                                         inputField: Int @test
                                       }

                                       type TestOutput implements TestInterface @test {
                                         field: Int @test
                                       }

                                       interface TestInterface @test {
                                         field: Int @test
                                       }

                                       enum TestEnum @test {
                                         ENUM_VALUE @test
                                       }

                                       union TestUnion @test = TestOutput

                                       scalar Test @test

                                       "A test directive"
                                       directive @test repeatable on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
                                       """)
                .AddType(new AnyType("Test"))
                .AddResolverMocking());

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        var fusionGraphDoc = Utf8GraphQLParser.Parse(SchemaFormatter.FormatAsString(fusionGraph));
        var typeNames = FusionTypeNames.From(fusionGraphDoc);
        var rewriter = new FusionGraphConfigurationToSchemaRewriter();
        var schemaDoc = (DocumentNode)rewriter.Rewrite(fusionGraphDoc, new(typeNames))!;

        schemaDoc.MatchSnapshot(extension: ".graphql");
    }
}
