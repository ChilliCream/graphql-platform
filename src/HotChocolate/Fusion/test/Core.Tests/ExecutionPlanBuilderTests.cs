using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
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
                @abc_source(schema: ""a"")
                @abc_fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @abc_fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @abc_variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @abc_variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @abc_fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @abc_fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @abc_source(schema: ""a"")
                @abc_source(schema: ""b"")
              name: String!
                @abc_source(schema: ""a"")
              bio: String
                @abc_source(schema: ""b"")
            }

            schema
              @fusion(prefix: ""abc"")
              @abc_httpClient(schema: ""a"" baseAddress: ""https://a/graphql"")
              @abc_httpClient(schema: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = ServiceConfiguration.Load(serviceDefinition);

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
        var requestPlaner = new RequestPlanner(serviceConfig);
        var requirementsPlaner = new RequirementsPlanner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");
        snapshot.Add(queryPlan, "Query Plan");
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
                @abc_source(schema: ""a"")
                @abc_fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @abc_fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @abc_variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @abc_variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @abc_fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @abc_fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @abc_source(schema: ""a"")
                @abc_source(schema: ""b"")
              name: String!
                @abc_source(schema: ""a"")
              bio: String
                @abc_source(schema: ""b"")
            }

            schema
              @abc_fusion(prefix: ""abc"", prefixSelf: true)
              @abc_httpClient(schema: ""a"" baseAddress: ""https://a/graphql"")
              @abc_httpClient(schema: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = ServiceConfiguration.Load(serviceDefinition);

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
        var requestPlaner = new RequestPlanner(serviceConfig);
        var requirementsPlaner = new RequirementsPlanner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");
        snapshot.Add(queryPlan, "Query Plan");
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
                @source(schema: ""a"")
                @fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @source(schema: ""a"")
                @source(schema: ""b"")
              name: String!
                @source(schema: ""a"")
              bio: String
                @source(schema: ""b"")
            }

            schema
              @httpClient(schema: ""a"" baseAddress: ""https://a/graphql"")
              @httpClient(schema: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = ServiceConfiguration.Load(serviceDefinition);

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
        var requestPlaner = new RequestPlanner(serviceConfig);
        var requirementsPlaner = new RequirementsPlanner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");
        snapshot.Add(queryPlan, "Query Plan");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Use_Alias_GetPersonById_With_Name_And_Bio()
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
                @source(schema: ""a"")
                @fetch(schema: ""a"", select: ""personByIdFoo(id: $personId) { ... Person }"")
                @fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @source(schema: ""a"")
                @source(schema: ""b"")
              name: String!
                @source(schema: ""a"")
              bio: String
                @source(schema: ""b"")
            }

            schema
              @httpClient(schema: ""a"" baseAddress: ""https://a/graphql"")
              @httpClient(schema: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = ServiceConfiguration.Load(serviceDefinition);

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
        var requestPlaner = new RequestPlanner(serviceConfig);
        var requirementsPlaner = new RequirementsPlanner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");
        snapshot.Add(queryPlan, "Query Plan");
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
                @fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @source(schema: ""a"")
                @source(schema: ""b"")
              name: String!
                @source(schema: ""a"")
              bio: String
                @source(schema: ""b"")
            }

            schema
              @httpClient(schema: ""a"" baseAddress: ""https://a/graphql"")
              @httpClient(schema: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = ServiceConfiguration.Load(serviceDefinition);

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
        var requestPlaner = new RequestPlanner(serviceConfig);
        var requirementsPlaner = new RequirementsPlanner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");
        snapshot.Add(queryPlan, "Query Plan");
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
                @fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @variable(name: ""personId"", select: ""id"" schema: ""a"" type: ""ID!"")
              @variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @source(schema: ""a"")
                @source(schema: ""b"")
                @source(schema: ""c"")
              name: String!
                @source(schema: ""a"")
              bio: String
                @source(schema: ""b"")
              friends: [Person!]
                @source(schema: ""a"")
            }

            schema
              @httpClient(schema: ""a"" baseAddress: ""https://a/graphql"")
              @httpClient(schema: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = ServiceConfiguration.Load(serviceDefinition);

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
        var requestPlaner = new RequestPlanner(serviceConfig);
        var requirementsPlaner = new RequirementsPlanner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");
        snapshot.Add(queryPlan, "Query Plan");
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
                @fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @variable(name: ""personId"", select: ""id"" schema: ""a"" type: ""ID!"")
              @variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @source(schema: ""a"")
                @source(schema: ""b"")
                @source(schema: ""c"")
              name: String!
                @source(schema: ""a"")
              bio: String
                @source(schema: ""b"")
              friends: [Person!]
                @source(schema: ""a"")
            }

            schema
              @httpClient(schema: ""a"" baseAddress: ""https://a/graphql"")
              @httpClient(schema: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = ServiceConfiguration.Load(serviceDefinition);

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
        var requestPlaner = new RequestPlanner(serviceConfig);
        var requirementsPlaner = new RequirementsPlanner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");
        snapshot.Add(queryPlan, "Query Plan");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task StoreService_Me_Name_Reviews_Upc()
    {
        // arrange
        var request = Parse(
            @"query Me {
                me {
                    name
                    reviews {
                        nodes {
                            product {
                                upc
                            }
                        }
                    }
                }
            }");

        // act
        var queryPlan = await BuildStoreServiceQueryPlanAsync(request);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");
        snapshot.Add(queryPlan, "Query Plan");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task StoreService_Selection_With_Arguments()
    {
        // arrange
        var request = Parse(
            @"query Me {
                me {
                    name
                    reviews(first: 1) {
                        nodes {
                            product {
                                upc
                            }
                        }
                    }
                }
            }");

        // act
        var queryPlan = await BuildStoreServiceQueryPlanAsync(request);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");
        snapshot.Add(queryPlan, "Query Plan");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task StoreService_Introspection()
    {
        // arrange
        var request = Parse(
            @"query Intro {
                __schema {
                    types {
                        name
                    }
                }
            }");

        // act
        var queryPlan = await BuildStoreServiceQueryPlanAsync(request);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");
        snapshot.Add(queryPlan, "Query Plan");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task StoreService_Introspection_TypeName()
    {
        // arrange
        var request = Parse(
            @"query Me {
                me {
                    name
                    reviews {
                        nodes {
                            product {
                                __typename
                            }
                        }
                    }
                }
            }");

        // act
        var queryPlan = await BuildStoreServiceQueryPlanAsync(request);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");
        snapshot.Add(queryPlan, "Query Plan");
        await snapshot.MatchAsync();
    }

    private static async Task<QueryPlan> BuildStoreServiceQueryPlanAsync(DocumentNode request)
    {
        // arrange
        var serviceConfigDoc = Parse(FileResource.Open("StoreServiceConfig.graphql")!);
        var serviceConfig = ServiceConfiguration.Load(serviceConfigDoc);
        var context = ConfigurationDirectiveNamesContext.From(serviceConfigDoc);
        var rewriter = new ServiceConfigurationToSchemaRewriter();
        var rewritten = rewriter.Rewrite(serviceConfigDoc, context);
        var sdl = rewritten!.ToString();

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(n => n)
            .BuildSchemaAsync();

        var operationCompiler = new OperationCompiler(new());
        var operation = operationCompiler.Compile(
            "abc",
            (OperationDefinitionNode)request.Definitions[0],
            schema.QueryType,
            request,
            schema);

        // act
        var queryPlanContext = new QueryPlanContext(operation);
        var requestPlaner = new RequestPlanner(serviceConfig);
        var requirementsPlaner = new RequirementsPlanner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        return executionPlanBuilder.Build(queryPlanContext);
    }
}
