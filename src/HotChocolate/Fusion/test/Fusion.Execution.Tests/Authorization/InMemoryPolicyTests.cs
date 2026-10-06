using HotChocolate.Execution;
using HotChocolate.Fusion.Authorization.InMemory;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Authorization.PolicyTestHelper;

namespace HotChocolate.Fusion.Authorization;

public class InMemoryPolicyTests : FusionTestBase
{
    [Fact]
    public async Task AddInMemoryPolicies_Should_AnswerConfiguredVerdicts_When_PoliciesAreResolved()
    {
        // arrange
        var (resolver, _) = await CreateResolverAsync(
            policies => policies
                .Allow("allowed")
                .Deny("denied", "no")
                .Evaluate("odd", (_, entry) => entry.Selection.Id % 2 == 1));
        var selections = CreateSelections();

        // act
        var allowed = await EvaluateAsync(resolver, "allowed", selections[1], selections[2]);
        var denied = await EvaluateAsync(resolver, "denied", selections[1]);
        var odd = await EvaluateAsync(resolver, "odd", selections[0], selections[1], selections[2]);

        // assert
        Assert.Equal([PolicyOutcome.Allowed, PolicyOutcome.Allowed], allowed.Select(v => v.Outcome));
        Assert.Equal([new PolicyVerdict(PolicyOutcome.Denied, "no", null)], denied);
        Assert.Equal(
            selections.Select(s => s.Id % 2 == 1 ? PolicyOutcome.Allowed : PolicyOutcome.Denied),
            odd.Select(v => v.Outcome));
    }

    [Fact]
    public async Task AddInMemoryPolicies_Should_LeaveEntriesUnanswered_When_PolicyIsConfiguredUnanswered()
    {
        // arrange
        var (resolver, _) = await CreateResolverAsync(policies => policies.Unanswered("silent"));
        var selections = CreateSelections();

        // act
        var verdicts = await EvaluateAsync(resolver, "silent", selections[1], selections[2]);

        // assert
        Assert.Equal(
            [PolicyOutcome.Unanswered, PolicyOutcome.Unanswered],
            verdicts.Select(v => v.Outcome));
    }

    [Fact]
    public async Task Resolve_Should_ReturnNull_When_PolicyNameIsNotConfigured()
    {
        // arrange
        var (resolver, _) = await CreateResolverAsync(policies => policies.Allow("known"));

        // act
        var policy = resolver.Resolve("unknown", DirectiveNames.Policy.Name);

        // assert
        Assert.Null(policy);
    }

    [Fact]
    public async Task Resolve_Should_FallThroughToSecondInMemoryProvider_When_FirstDoesNotKnowTheName()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .AddInMemoryPolicies(policies => policies.Allow("a"))
            .AddInMemoryPolicies(policies => policies.Deny("b"))
            .AddInMemoryConfiguration(ComposeSchemaDocument("type Query { field: String! }"));
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        var executor = await serviceProvider.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var resolver = executor.Schema.Services.GetRequiredService<IPolicyResolver>();
        var selections = CreateSelections();

        // act
        var verdicts = await EvaluateAsync(resolver, "b", selections[1]);

        // assert
        Assert.Equal([PolicyOutcome.Denied], verdicts.Select(v => v.Outcome));
    }

    [Fact]
    public async Task Recorder_Should_RecordEntriesInEvaluationOrder_When_PoliciesAreEvaluated()
    {
        // arrange
        var (resolver, recorder) = await CreateResolverAsync(
            policies => policies.Allow("a").Deny("b"));
        var selections = CreateSelections();

        // act
        await EvaluateAsync(resolver, "b", selections[2]);
        await EvaluateAsync(resolver, "a", selections[0], selections[1]);

        // assert
        Assert.Equal(
            [("b", selections[2].Id), ("a", selections[0].Id), ("a", selections[1].Id)],
            recorder.Records.Select(r => (r.PolicyName, r.Entry.Selection.Id)));
    }

    [Fact]
    public async Task Recorder_Should_BeEmpty_When_ClearIsCalled()
    {
        // arrange
        var (resolver, recorder) = await CreateResolverAsync(policies => policies.Allow("a"));
        var selections = CreateSelections();
        await EvaluateAsync(resolver, "a", selections[1]);

        // act
        recorder.Clear();

        // assert
        Assert.Empty(recorder.Records);
    }

    [Fact]
    public async Task Resolve_Should_ReturnNull_When_InMemoryPoliciesAreNotRegistered()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(ComposeSchemaDocument("type Query { field: String! }"));
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        var executor = await serviceProvider.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var resolver = executor.Schema.Services.GetService<IPolicyResolver>();

        // assert
        Assert.Null(resolver);
    }

    private async Task<(IPolicyResolver Resolver, InMemoryPolicyRecorder Recorder)> CreateResolverAsync(
        Action<InMemoryPolicyBuilder> configure)
    {
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .AddInMemoryPolicies(configure)
            .AddInMemoryConfiguration(ComposeSchemaDocument("type Query { field: String! }"));

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        var executor = await serviceProvider.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        return (
            executor.Schema.Services.GetRequiredService<IPolicyResolver>(),
            serviceProvider.GetRequiredService<InMemoryPolicyRecorder>());
    }

    private static async Task<PolicyVerdict[]> EvaluateAsync(
        IPolicyResolver resolver,
        string policyName,
        params ISelection[] selections)
    {
        var policy = resolver.Resolve(policyName, DirectiveNames.Policy.Name);
        Assert.NotNull(policy);

        var context = CreateContext(
            Authenticated(),
            selections.Select(s => CreateEntry(s, policy, policyName: policyName)).ToArray());

        await policy.EvaluateAsync(context, CancellationToken.None);
        return context.Verdicts.ToArray();
    }
}
