using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Shared;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using static HotChocolate.Language.Utf8GraphQLParser;
using HttpClientConfiguration = HotChocolate.Fusion.Composition.HttpClientConfiguration;

namespace HotChocolate.Fusion;

public class RequireTests
{
    [Fact]
    public async Task Require_On_MutationPayload()
    {
        // arrange
        var subgraphA = DemoSubgraph.CreateSubgraphConfig(
            "subgraphA",
            new Uri("http://localhost:5001"),
            """
            type User {
                id: ID!
                someField: String!
            }

            type Query {
                userById(id: ID!): User
            }
            """
        );

        var subgraphB = DemoSubgraph.CreateSubgraphConfig(
            "subgraphB",
            new Uri("http://localhost:5002"),
            """
            type User {
                id: ID!
                nestedField(someField: String! @require(field: "someField")): NestedType!
            }

            type NestedType {
                otherField: Int!
            }

            type Mutation {
                createUser: CreateUserPayload
            }

            type CreateUserPayload {
                user: User!
            }

            type Query {
                userById(id: ID!): User @lookup @internal
            }
            """
        );

        var fusionGraph = await FusionGraphComposer.ComposeAsync([subgraphA, subgraphB]);

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            mutation {
                createUser {
                    user {
                        nestedField {
                            otherField
                        }
                    }
                }
            }

            """
        );

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchMarkdownAsync();
    }

    private static async Task<(
        DocumentNode UserRequest,
        Execution.Nodes.QueryPlan QueryPlan
    )> CreateQueryPlanAsync(
        Skimmed.SchemaDefinition fusionGraph,
        [StringSyntax("graphql")] string query
    )
    {
        var document = SchemaFormatter.FormatAsDocument(fusionGraph);
        var context = FusionTypeNames.From(document);
        var rewriter = new FusionGraphConfigurationToSchemaRewriter();
        var rewritten = rewriter.Rewrite(document, new(context))!;

        var services = new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(rewritten.ToString())
            .UseField(n => n);

        if (document.Definitions.Any(d => d is ScalarTypeDefinitionNode { Name.Value: "Upload" }))
        {
            services.AddUploadType();
        }

        var schema = await services.BuildSchemaAsync();
        var serviceConfig = FusionGraphConfiguration.Load(document);

        var request = Parse(query);

        var operationCompiler = new OperationCompiler(new());
        var operationDef = (OperationDefinitionNode)request.Definitions[0];
        var operation = operationCompiler.Compile(
            new OperationCompilerRequest(
                "abc",
                request,
                operationDef,
                schema.GetOperationType(operationDef.Operation)!,
                schema
            )
        );

        var queryPlanner = new QueryPlanner(serviceConfig, schema);
        var queryPlan = queryPlanner.Plan(operation);

        return (request, queryPlan);
    }

    private static IClientConfiguration[] CreateClients() =>
        [new HttpClientConfiguration(new Uri("http://nothing"))];
}
