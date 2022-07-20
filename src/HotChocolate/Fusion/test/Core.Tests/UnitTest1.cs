using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace test;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        // arrange
        var sdl = @"
            type Query {
                personById(id: ID!) : Person
            }

            type Person {
                id: ID!
                name: String!
                bio: String
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var distributedQueryType = new ObjectType(
            "Query",
            new[]
            {
                new MemberBinding("a", "Query"),
                new MemberBinding("b", "Query"),
            },
            Array.Empty<FetchDefinition>(),
            new[]
            {
                new ObjectField(
                    "personById",
                    new[] { new MemberBinding("a", "personById"), },
                    new[]
                    {
                        new FetchDefinition(
                            "a",
                            Syntax.ParseSelectionSet("{ personById }"),
                            null,
                            Array.Empty<string>())
                    })
            });

        var distributedPersonType = new ObjectType(
            "Person",
            new[]
            {
                new MemberBinding("a", "Person"),
                new MemberBinding("b", "Person"),
            },
            Array.Empty<FetchDefinition>(),
            new[]
            {
                new ObjectField(
                    "id",
                    new[]
                    {
                        new MemberBinding("a", "id"),
                        new MemberBinding("b", "id"),
                    },
                    Array.Empty<FetchDefinition>()),
                new ObjectField(
                    "name",
                    new[] { new MemberBinding("a", "name"), },
                    Array.Empty<FetchDefinition>()),
                new ObjectField(
                    "bio",
                    new[] { new MemberBinding("b", "bio"), },
                    Array.Empty<FetchDefinition>())
            });

        var distributedSchema = new Schema(
            new[] { "a", "b" },
            new[]
            {
                distributedQueryType,
                distributedPersonType
            });

        var request =
            Parse(
                @"{
                    personById(id: 1) {
                        id
                        name
                        bio
                    }
                }");

        var operationCompiler = new OperationCompiler(new());
        var operation = operationCompiler.Compile(
            "abc",
            (OperationDefinitionNode)request.Definitions.First(),
            schema.QueryType,
            request,
            schema);

        // act
        var queryPlanBuilder = new QueryPlanBuilder(distributedSchema);

        queryPlanBuilder.CollectSelectionsBySchema1(
            operation,
            operation.RootSelectionSet,
            distributedQueryType);

    }
}
