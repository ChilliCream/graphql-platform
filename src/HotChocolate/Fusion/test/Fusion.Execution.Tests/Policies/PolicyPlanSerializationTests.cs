using System.Security.Claims;
using System.Text;
using HotChocolate.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Policies.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Policies;

public class PolicyPlanSerializationTests : AuthorizationExecutionTestBase
{
    private const string Schema =
        """
        type Query {
          product: Product
        }

        type Product {
          id: ID!
          name: String @authenticated
          price: Int @requiresScopes(scopes: [["read"]])
          margin: Int @policy(policies: [["finance"]])
        }
        """;

    private const string GhostSchema =
        """
        type Query {
          ghost: String @policy(policies: [["ghost"]])
        }
        """;

    private const string Data =
        """
        {
          "product": { "id": "1", "name": "Shoe", "price": 10, "margin": 3 },
          "ghost": "gh"
        }
        """;

    [Fact]
    public async Task ExecuteAsync_Should_ReturnPlannedResult_When_ParsedPlanRunsAsAnonymousUser()
    {
        // arrange
        var request = CreateRequest("{ product { id name price margin } }").Build();

        // act
        var (planned, parsed) = await ExecutePlannedAndParsedAsync(Schema, request);

        // assert
        Assert.Equal(planned, parsed);
        parsed.MatchInlineSnapshot(
            """
            {
              "data": {
                "product": {
                  "id": "1",
                  "name": null,
                  "price": null,
                  "margin": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnPlannedResult_When_ParsedPlanRunsAsUserWithoutRequiredScope()
    {
        // arrange
        var user = Authenticated(new Claim("scope", "write"));
        var request = CreateRequest("{ product { id name price margin } }", user).Build();

        // act
        var (planned, parsed) = await ExecutePlannedAndParsedAsync(
            Schema,
            request,
            policies => policies.Allow("finance"));

        // assert
        Assert.Equal(planned, parsed);
        parsed.MatchInlineSnapshot(
            """
            {
              "data": {
                "product": {
                  "id": "1",
                  "name": "Shoe",
                  "price": null,
                  "margin": 3
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_FailTheRequest_When_ParsedPlanHasPolicyThatNoProviderKnows()
    {
        // arrange
        var request = CreateRequest("{ ghost }", Authenticated()).Build();

        // act
        var (planned, parsed) = await ExecutePlannedAndParsedAsync(
            GhostSchema,
            request,
            configure: builder => builder.ModifyAuthorizationOptions(
                options => options.DisableAuthorizationValidation = true));

        // assert
        Assert.Equal(planned, parsed);
        parsed.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The authorization of the request could not be evaluated."
                }
              ]
            }
            """);
    }

    // Plans and executes the request, then formats the plan, parses it with the services of a
    // second gateway and executes the same request with the parsed plan in place of planning.
    private static async Task<(string Planned, string Parsed)> ExecutePlannedAndParsedAsync(
        string sourceSchema,
        IOperationRequest request,
        Action<InMemoryPolicyBuilder>? policies = null,
        Action<IFusionGatewayBuilder>? configure = null)
    {
        var plannedCapture = new PlanCapture();
        var plannedExecutor = await CreateExecutorAsync(
            sourceSchema,
            new AuthorizationTestClient(Data),
            new InMemoryPolicyRecorder(),
            policies,
            builder =>
            {
                configure?.Invoke(builder);
                builder.AddOperationPlannerInterceptor(_ => plannedCapture);
            });

        await using var plannedResult = await plannedExecutor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);
        var plan = Assert.IsType<OperationPlan>(plannedCapture.Plan);
        var planJson = new JsonOperationPlanFormatter().Format(plan);

        var parsedCapture = new PlanCapture();
        var parsedExecutor = await CreateExecutorAsync(
            sourceSchema,
            new AuthorizationTestClient(Data),
            new InMemoryPolicyRecorder(),
            policies,
            builder =>
            {
                configure?.Invoke(builder);
                builder.AddOperationPlannerInterceptor(_ => parsedCapture);
            });

        var services = parsedExecutor.Schema.Services;
        var parser = new JsonOperationPlanParser(
            services.GetRequiredService<OperationCompiler>(),
            services.GetRequiredService<IPolicyResolver>());
        var parsedPlan = parser.Parse(Encoding.UTF8.GetBytes(planJson));
        services.GetRequiredService<Cache<OperationPlan>>().TryAdd(
            Assert.IsType<string>(plannedCapture.OperationId),
            parsedPlan);

        await using var parsedResult = await parsedExecutor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // The parsed plan must have served the request without planning it again.
        Assert.Null(parsedCapture.Plan);

        return (
            plannedResult.ExpectOperationResult().ToJson(),
            parsedResult.ExpectOperationResult().ToJson());
    }

    private sealed class PlanCapture : IOperationPlannerInterceptor
    {
        public string? OperationId { get; private set; }

        public OperationPlan? Plan { get; private set; }

        public void OnAfterPlanCompleted(
            OperationDocumentInfo operationDocumentInfo,
            OperationPlan operationPlan)
        {
            OperationId = operationDocumentInfo.OperationId;
            Plan = operationPlan;
        }
    }
}
