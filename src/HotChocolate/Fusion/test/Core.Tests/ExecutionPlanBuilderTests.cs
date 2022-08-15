using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class ExecutionPlanBuilderTests
{
    [Fact]
    public async Task GetPersonById_With_Name_And_Bio_With_Prefixed_Directives()
    {
        // arrange
        const string sdl = @"
            type Query {
                personById(id: ID!) : Person
            }

            type Person {
                id: ID!
                name: String!
                bio: String
            }";

        const string serviceDefinition = @"
            type Query {
              personById(id: ID!): Person
                @abc_variable(name: ""personId"", argument: ""id"")
                @abc_bind(to: ""a"")
                @abc_fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @abc_fetch(from: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @abc_variable(name: ""personId"", select: ""id"" from: ""b"" type: ""ID!"")
              @abc_variable(name: ""personId"", select: ""id"" from: ""b"" type: ""ID!"")
              @abc_fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @abc_fetch(from: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @abc_bind(to: ""a"")
                @abc_bind(to: ""b"")
              name: String!
                @abc_bind(to: ""a"")
              bio: String
                @abc_bind(to: ""b"")
            }

            schema
              @fusion(prefix: ""abc"")
              @abc_httpClient(name: ""a"" baseAddress: ""https://a/graphql"")
              @abc_httpClient(name: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = Metadata.ServiceConfiguration.Load(serviceDefinition);

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
        var queryPlanContext = new QueryPlanContext(operation);
        var requestPlaner = new RequestPlaner(serviceConfig);
        var requirementsPlaner = new RequirementsPlaner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var index = 0;
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");

        foreach (var executionNode in queryPlan.ExecutionNodes)
        {
            if (executionNode is RequestNode rn)
            {
                snapshot.Add(rn.Handler.Document, $"Request {++index}");
            }
        }

        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task GetPersonById_With_Name_And_Bio_With_Prefixed_Directives_PrefixedSelf()
    {
        // arrange
        const string sdl = @"
            type Query {
                personById(id: ID!) : Person
            }

            type Person {
                id: ID!
                name: String!
                bio: String
            }";

        const string serviceDefinition = @"
            type Query {
              personById(id: ID!): Person
                @abc_variable(name: ""personId"", argument: ""id"")
                @abc_bind(to: ""a"")
                @abc_fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @abc_fetch(from: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @abc_variable(name: ""personId"", select: ""id"" from: ""b"" type: ""ID!"")
              @abc_variable(name: ""personId"", select: ""id"" from: ""b"" type: ""ID!"")
              @abc_fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @abc_fetch(from: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @abc_bind(to: ""a"")
                @abc_bind(to: ""b"")
              name: String!
                @abc_bind(to: ""a"")
              bio: String
                @abc_bind(to: ""b"")
            }

            schema
              @abc_fusion(prefix: ""abc"", prefixSelf: true)
              @abc_httpClient(name: ""a"" baseAddress: ""https://a/graphql"")
              @abc_httpClient(name: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = Metadata.ServiceConfiguration.Load(serviceDefinition);

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
        var queryPlanContext = new QueryPlanContext(operation);
        var requestPlaner = new RequestPlaner(serviceConfig);
        var requirementsPlaner = new RequirementsPlaner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var index = 0;
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");

        foreach (var executionNode in queryPlan.ExecutionNodes)
        {
            if (executionNode is RequestNode rn)
            {
                snapshot.Add(rn.Handler.Document, $"Request {++index}");
            }
        }

        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task GetPersonById_With_Name_And_Bio()
    {
        // arrange
        const string sdl = @"
            type Query {
                personById(id: ID!) : Person
            }

            type Person {
                id: ID!
                name: String!
                bio: String
            }";

        const string serviceDefinition = @"
            type Query {
              personById(id: ID!): Person
                @variable(name: ""personId"", argument: ""id"")
                @bind(to: ""a"")
                @fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @fetch(from: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @variable(name: ""personId"", select: ""id"" from: ""b"" type: ""ID!"")
              @variable(name: ""personId"", select: ""id"" from: ""b"" type: ""ID!"")
              @fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @fetch(from: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @bind(to: ""a"")
                @bind(to: ""b"")
              name: String!
                @bind(to: ""a"")
              bio: String
                @bind(to: ""b"")
            }

            schema
              @httpClient(name: ""a"" baseAddress: ""https://a/graphql"")
              @httpClient(name: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = Metadata.ServiceConfiguration.Load(serviceDefinition);

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
        var queryPlanContext = new QueryPlanContext(operation);
        var requestPlaner = new RequestPlaner(serviceConfig);
        var requirementsPlaner = new RequirementsPlaner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var index = 0;
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");

        foreach (var executionNode in queryPlan.ExecutionNodes)
        {
            if (executionNode is RequestNode rn)
            {
                snapshot.Add(rn.Handler.Document, $"Request {++index}");
            }
        }

        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task GetPersonById_With_Bio()
    {
        // arrange
        const string sdl = @"
            type Query {
                personById(id: ID!) : Person
            }

            type Person {
                id: ID!
                name: String!
                bio: String
            }";

        const string serviceDefinition = @"
            type Query {
              personById(id: ID!): Person
                @variable(name: ""personId"", argument: ""id"")
                @fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @fetch(from: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @variable(name: ""personId"", select: ""id"" from: ""b"" type: ""ID!"")
              @variable(name: ""personId"", select: ""id"" from: ""b"" type: ""ID!"")
              @fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @fetch(from: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @bind(to: ""a"")
                @bind(to: ""b"")
              name: String!
                @bind(to: ""a"")
              bio: String
                @bind(to: ""b"")
            }

            schema
              @httpClient(name: ""a"" baseAddress: ""https://a/graphql"")
              @httpClient(name: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = Metadata.ServiceConfiguration.Load(serviceDefinition);

        var request =
            Parse(
                @"query GetPersonById {
                    personById(id: 1) {
                        id
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
        var queryPlanContext = new QueryPlanContext(operation);
        var requestPlaner = new RequestPlaner(serviceConfig);
        var requirementsPlaner = new RequirementsPlaner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var index = 0;
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");

        foreach (var executionNode in queryPlan.ExecutionNodes)
        {
            if (executionNode is RequestNode rn)
            {
                snapshot.Add(rn.Handler.Document, $"Request {++index}");
            }
        }

        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task GetPersonById_With_Bio_Friends_Bio()
    {
        // arrange
        const string sdl = @"
            type Query {
                personById(id: ID!) : Person
            }

            type Person {
                id: ID!
                name: String!
                bio: String
                friends: [Person!]
            }";

        const string serviceDefinition = @"
            type Query {
              personById(id: ID!): Person
                @variable(name: ""personId"", argument: ""id"")
                @fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @fetch(from: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @variable(name: ""personId"", select: ""id"" from: ""a"" type: ""ID!"")
              @variable(name: ""personId"", select: ""id"" from: ""b"" type: ""ID!"")
              @fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @fetch(from: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @bind(to: ""a"")
                @bind(to: ""b"")
                @bind(to: ""c"")
              name: String!
                @bind(to: ""a"")
              bio: String
                @bind(to: ""b"")
              friends: [Person!]
                @bind(to: ""a"")
            }

            schema
              @httpClient(name: ""a"" baseAddress: ""https://a/graphql"")
              @httpClient(name: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = Metadata.ServiceConfiguration.Load(serviceDefinition);

        var request =
            Parse(
                @"query GetPersonById {
                    personById(id: 1) {
                        bio
                        friends {
                            bio
                        }
                    }
                }");

        var operationCompiler = new OperationCompiler(new());
        var operation = operationCompiler.Compile(
            "abc",
            (OperationDefinitionNode)request.Definitions[0],
            schema.QueryType,
            request,
            schema);

        // act
        var queryPlanContext = new QueryPlanContext(operation);
        var requestPlaner = new RequestPlaner(serviceConfig);
        var requirementsPlaner = new RequirementsPlaner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var index = 0;
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");

        foreach (var executionNode in queryPlan.ExecutionNodes)
        {
            if (executionNode is RequestNode rn)
            {
                snapshot.Add(rn.Handler.Document, $"Request {++index}");
            }
        }

        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task GetPersonById_With_Name_Friends_Name_Bio()
    {
        // arrange
        const string sdl = @"
            type Query {
                personById(id: ID!) : Person
            }

            type Person {
                id: ID!
                name: String!
                bio: String
                friends: [Person!]
            }";

        const string serviceDefinition = @"
            type Query {
              personById(id: ID!): Person
                @variable(name: ""personId"", argument: ""id"")
                @fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @fetch(from: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @variable(name: ""personId"", select: ""id"" from: ""a"" type: ""ID!"")
              @variable(name: ""personId"", select: ""id"" from: ""b"" type: ""ID!"")
              @fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @fetch(from: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @bind(to: ""a"")
                @bind(to: ""b"")
                @bind(to: ""c"")
              name: String!
                @bind(to: ""a"")
              bio: String
                @bind(to: ""b"")
              friends: [Person!]
                @bind(to: ""a"")
            }

            schema
              @httpClient(name: ""a"" baseAddress: ""https://a/graphql"")
              @httpClient(name: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = Metadata.ServiceConfiguration.Load(serviceDefinition);

        var request =
            Parse(
                @"query GetPersonById {
                    personById(id: 1) {
                        name
                        friends {
                            name
                            bio
                        }
                    }
                }");

        var operationCompiler = new OperationCompiler(new());
        var operation = operationCompiler.Compile(
            "abc",
            (OperationDefinitionNode)request.Definitions[0],
            schema.QueryType,
            request,
            schema);

        // act
        var queryPlanContext = new QueryPlanContext(operation);
        var requestPlaner = new RequestPlaner(serviceConfig);
        var requirementsPlaner = new RequirementsPlaner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var index = 0;
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");

        foreach (var executionNode in queryPlan.ExecutionNodes)
        {
            if (executionNode is RequestNode rn)
            {
                snapshot.Add(rn.Handler.Document, $"Request {++index}");
            }
        }

        await snapshot.MatchAsync();
    }
}
