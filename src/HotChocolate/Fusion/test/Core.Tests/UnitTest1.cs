using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;
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
                            new[] { "personId" })
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
                    new[] { "personId" })
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
        var queryPlanContext = new QueryPlanContext(operation);
        var requestPlaner = new RequestPlaner(distributedSchema);
        var requirementsPlaner = new RequirementsPlaner();
        var executionPlanBuilder = new ExecutionPlanBuilder(distributedSchema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var documents = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var index = 0;
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");

        foreach (var document in documents)
        {
            snapshot.Add(document, $"Request {++index}");
        }

        await snapshot.MatchAsync();
    }
}
