using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Language.Utf8GraphQLParser;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;
using ObjectField = HotChocolate.Fusion.Metadata.ObjectField;
using ObjectType = HotChocolate.Fusion.Metadata.ObjectType;

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
                        new ArgumentVariableDefinition(
                            "personId",
                            ParseTypeReference("ID"),
                            "id")
                    },
                    new[]
                    {
                        new FetchDefinition(
                            "a",
                            ParseField("internalPersonById(id: $personId)"),
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
            new[]
            {
                new FetchDefinition(
                    "b",
                    ParseField("personById(id: $personId)"),
                    null,
                    Array.Empty<string>())
            },
            new[]
            {
                new ObjectField(
                    "id",
                    new[]
                    {
                        new MemberBinding("a", "id"),
                        new MemberBinding("b", "id"),
                    },
                    Array.Empty<ArgumentVariableDefinition>(),
                    Array.Empty<FetchDefinition>()),
                new ObjectField(
                    "name",
                    new[] { new MemberBinding("a", "name"), },
                    Array.Empty<ArgumentVariableDefinition>(),
                    Array.Empty<FetchDefinition>()),
                new ObjectField(
                    "bio",
                    new[] { new MemberBinding("b", "bio"), },
                    Array.Empty<ArgumentVariableDefinition>(),
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
                @"query GetPersonById {
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
        // var queryPlan = new QueryPlan();
        // var queryPlanBuilder = new QueryPlanBuilder(distributedSchema, operation);
        var inspector = new OperationInspector(distributedSchema);
        var result = inspector.Inspect(operation);

        // assert
        /*
        await Snapshot
            .Create()
            .Add(request, "User Request")
            .Add(((RequestNode)queryPlan.Nodes[0]).Handler.Document, "Request 1")
            .MatchAsync();*/
    }
}
