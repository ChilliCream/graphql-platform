using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using OperationRequest = HotChocolate.Transport.OperationRequest;
using OperationResult = HotChocolate.Transport.OperationResult;

namespace HotChocolate.Fusion;

public class ActionPolicyTests : FusionTestBase
{
    private const string SchemaText =
        """
        directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
          repeatable on OBJECT | FIELD_DEFINITION

        enum PolicyDenialBehavior { NULL ERROR ABORT }

        type Query {
          placeholder: String
        }

        type Mutation {
          write(value: Int!): String @policy(names: "CanWrite", onDenied: NULL)
        }
        """;

    [Fact]
    public async Task Mutation_With_Action_Policy_Denied_Does_Not_Invoke_Source()
    {
        // arrange
        using var server = CreateSourceSchema("A", SchemaText);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: builder =>
                builder.ConfigureSchemaServices(
                    (_, services) => services.AddSingleton<IPolicyProvider>(
                        new ActionPolicyProvider(new DenyOnArgumentPolicy("CanWrite", "value", "1")))));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("mutation { write(value: 1) }");

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);
        var results = await ReadResultsAsync(response);

        // assert
        var result = Assert.Single(results);
        Assert.False(gateway.Interactions.TryGetValue("A", out var interactions) && !interactions.IsEmpty);
        Assert.Equal(JsonValueKind.Null, result.Data.GetProperty("write").ValueKind);

        foreach (var current in results)
        {
            current.Dispose();
        }
    }

    [Fact]
    public async Task Mutation_With_Action_Policy_Allowed_Invokes_Source()
    {
        // arrange
        using var server = CreateSourceSchema("A", SchemaText);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: builder =>
                builder.ConfigureSchemaServices(
                    (_, services) => services.AddSingleton<IPolicyProvider>(
                        new ActionPolicyProvider(new DenyOnArgumentPolicy("CanWrite", "value", "1")))));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("mutation { write(value: 2) }");

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);
        var results = await ReadResultsAsync(response);

        // assert
        var result = Assert.Single(results);
        Assert.True(result.Errors.ValueKind is JsonValueKind.Undefined);
        Assert.True(gateway.Interactions.TryGetValue("A", out var interactions) && !interactions.IsEmpty);

        foreach (var current in results)
        {
            current.Dispose();
        }
    }

    private static async Task<List<OperationResult>> ReadResultsAsync(GraphQLHttpResponse response)
    {
        var results = new List<OperationResult>();

        await foreach (var result in response.ReadAsResultStreamAsync())
        {
            results.Add(result);
        }

        return results;
    }

    private sealed class DenyOnArgumentPolicy(string name, string argumentName, string deniedRawValue) : IPolicy
    {
        public string Name => name;

        public PolicyRequirements Requirements { get; } =
            new() { Kind = PolicyEvaluationKind.ActionOccurrence };

        public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
        {
            var argument = context.Action!.Arguments.Fields
                .FirstOrDefault(field => field.Name.Value.Equals(argumentName, StringComparison.Ordinal));

            if (argument is { Value: IValueNode value } && value.Print(indented: false) == deniedRawValue)
            {
                context.Deny(0, "denied by test action policy");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class ActionPolicyProvider(params IPolicy[] policies) : IPolicyProvider
    {
        private readonly ImmutableArray<IPolicy> _policies = [.. policies];

        public IDisposable Subscribe(IObserver<ImmutableArray<IPolicy>> observer)
        {
            observer.OnNext(_policies);
            return NullSubscription.Instance;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private sealed class NullSubscription : IDisposable
        {
            public static readonly NullSubscription Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
