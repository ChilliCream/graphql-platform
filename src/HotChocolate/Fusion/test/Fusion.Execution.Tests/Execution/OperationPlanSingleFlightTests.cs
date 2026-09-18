using System.Collections.Concurrent;
using System.Diagnostics;
using HotChocolate.Caching.Memory;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Caching;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Pipeline;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanSingleFlightTests : FusionTestBase
{
    private const string VariableCostSchema =
        """
        directive @listSize(assumedSize: Int, slicingArguments: [String!], sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION

        type Query {
          items(n: Int!): [Item] @listSize(slicingArguments: ["n"])
        }

        type Item {
          value: String
        }
        """;

    private const string VariableCostOperation =
        """
        query Items($n: Int!) {
          items(n: $n) {
            value
          }
        }
        """;

    [Fact]
    public async Task Concurrent_Same_Operation_Should_Be_Coalesced_To_One_Planning_Run()
    {
        // arrange
        const int requestCount = 8;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var gate = new RequestGate(requestCount);

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => CreateGateMiddleware(next, gate),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateSingleFlightLeaderDelayMiddleware(next, TimeSpan.FromMilliseconds(100)),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        // act
        const string operationText =
            """
            query SameOpCoalesce {
              foo
            }
            """;
        var results = await Task.WhenAll(
            Enumerable.Range(0, requestCount)
                .Select(_ => executor.ExecuteAsync(operationText, cts.Token)));

        // assert
        Assert.All(results, t => Assert.Empty(t.ExpectOperationResult().Errors));

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(1, listener.PlanStartCount(operationId));
    }

    [Fact]
    public async Task Follower_Released_At_Plan_Set_Time_Never_Normalizes()
    {
        // arrange
        // Variable coercion and cost analysis run ahead of the plan cache and each ask for
        // the normalized document on their own, independently of any single-flight
        // coalescing; leaving both out of this pipeline isolates the one normalizer call
        // that planning itself needs, so the assertion below is exact: a follower released
        // at plan-set time - before it would ever reach OperationPlanMiddleware's own
        // normalization call - must not add a second call of its own.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var operationIds = new ConcurrentBag<string>();
        var leaderGate = new SingleFlightLeaderGate();
        var secondRequestObserver = new SecondRequestObserver();
        var normalizeCallCount = 0;

        var services = new ServiceCollection();
        var builder = services.AddGraphQLGateway();
        FusionSetupUtilities.ClearPipeline(builder);

        var executor = await builder
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationPlanCache()
            .UseOperationPlan()
            .UseSkipWarmupExecution()
            .UseConcurrencyGate()
            .UseOperationExecution()
            .ConfigureSchemaServices((_, schemaServices) =>
            {
                schemaServices.RemoveAll<IOperationDocumentNormalizer>();
                schemaServices.AddSingleton<IOperationDocumentNormalizer>(
                    sp => new CountingOperationDocumentNormalizer(
                        new OperationDocumentNormalizer(
                            sp.GetRequiredService<FusionSchemaDefinition>(),
                            sp.GetRequiredService<NormalizedDocumentCache>()),
                        () => Interlocked.Increment(ref normalizeCallCount)));
            })
            .UseRequest(
                (_, next) => CreateSecondRequestEnteredDownstreamMiddleware(next, secondRequestObserver),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateSingleFlightLeaderBlockMiddleware(next, leaderGate),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        const string operationText =
            """
            query FollowerNeverNormalizes {
              foo
            }
            """;

        // act
        var leaderTask = executor.ExecuteAsync(operationText, cts.Token);
        await leaderGate.WaitForEntryAsync(cts.Token);

        var followerTask = executor.ExecuteAsync(operationText, cts.Token);
        await secondRequestObserver.WaitForSecondRequestEnteredDownstreamAsync(cts.Token);

        leaderGate.Release();
        var results = await Task.WhenAll(leaderTask, followerTask);

        // assert
        Assert.All(results, t => Assert.Empty(t.ExpectOperationResult().Errors));
        Assert.Single(operationIds.Distinct());

        // the leader normalizes once, for its own plan; the follower is released the
        // moment the leader's plan is set and never calls the normalizer itself.
        Assert.Equal(1, Volatile.Read(ref normalizeCallCount));
    }

    [Fact]
    public async Task Concurrent_Distinct_Operations_Should_Not_Be_Coalesced()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var gate = new RequestGate(expectedRequests: 2);

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => CreateGateMiddleware(next, gate),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        const string operationText1 =
            """
            query DistinctOpOne {
              foo
            }
            """;
        const string operationText2 =
            """
            query DistinctOpTwo {
              __typename
            }
            """;

        // act
        var results = await Task.WhenAll(
            executor.ExecuteAsync(operationText1, cts.Token),
            executor.ExecuteAsync(operationText2, cts.Token));

        // assert
        Assert.All(results, t => Assert.Empty(t.ExpectOperationResult().Errors));

        var ids = operationIds.Distinct().ToArray();
        Assert.Equal(2, ids.Length);
        Assert.All(ids, id => Assert.Equal(1, listener.PlanStartCount(id)));
    }

    [Fact]
    public async Task Leader_Planning_Failure_Should_Be_Observed_By_Followers()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var leaderGate = new SingleFlightLeaderGate();
        var secondRequestObserver = new SecondRequestObserver();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .ModifyPlannerOptions(o => o.MaxPlanningTime = TimeSpan.FromTicks(1))
            .UseRequest(
                (_, next) => CreateSecondRequestEnteredDownstreamMiddleware(next, secondRequestObserver),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateSingleFlightLeaderBlockMiddleware(next, leaderGate),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        const string operationText =
            """
            query FailureCoalesce {
              foo
            }
            """;

        // act
        var leaderTask = executor.ExecuteAsync(operationText, cts.Token);
        await leaderGate.WaitForEntryAsync(cts.Token);

        var followerTask = executor.ExecuteAsync(operationText, cts.Token);
        await secondRequestObserver.WaitForSecondRequestEnteredDownstreamAsync(cts.Token);
        leaderGate.Release();

        var results = await Task.WhenAll(
            leaderTask,
            followerTask);

        // assert
        Assert.All(results, t => Assert.NotEmpty(t.ExpectOperationResult().Errors));

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(1, listener.PlanStartCount(operationId));
        Assert.Equal(1, listener.PlanErrorCount(operationId));
    }

    [Fact]
    public async Task RejectedRequest_Should_Not_Create_An_InFlight_Entry()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        const int rejectedRequestCount = 4;

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .ModifyCostOptions(options =>
            {
                options.MaxFieldCost = double.PositiveInfinity;
                options.MaxTypeCost = 10;
            })
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(ComposeSchemaDocument(1, VariableCostSchema))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);
        using var costlyRequest = CreateVariableCostRequest(1000);

        // act
        // cost analysis now runs before the plan cache, so a burst of concurrent, identical,
        // over-cost requests can never reach OperationPlanCacheMiddleware and therefore can
        // never register an in-flight entry that a later, affordable request for the same
        // operation would incorrectly coalesce onto.
        var rejectedResults = await Task.WhenAll(
            Enumerable.Range(0, rejectedRequestCount)
                .Select(_ => executor.ExecuteAsync(costlyRequest, cts.Token)));

        using var affordableRequest = CreateVariableCostRequest(1);
        var affordableResult = await executor.ExecuteAsync(affordableRequest, cts.Token);

        // assert
        Assert.All(rejectedResults, t => Assert.NotEmpty(t.ExpectOperationResult().Errors));
        Assert.Empty(affordableResult.ExpectOperationResult().Errors);

        // none of the rejected requests ever reached the plan cache / plan middleware
        var operationId = Assert.Single(operationIds);
        Assert.Equal(1, listener.PlanStartCount(operationId));

        var operationPlanCache = executor.Schema.Services.GetRequiredService<Cache<OperationPlan>>();
        Assert.Equal(1, operationPlanCache.Count);
    }

    [Fact]
    public async Task Followers_Should_Execute_While_Leader_Is_Still_Blocked_In_Execution()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var executionGate = new SingleFlightLeaderGate();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateSingleFlightLeaderBlockMiddleware(next, executionGate),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        const string operationText =
            """
            query FollowerRunsWhileLeaderExecutes {
              foo
            }
            """;

        // act
        var leaderTask = executor.ExecuteAsync(operationText, cts.Token);
        await executionGate.WaitForEntryAsync(cts.Token);

        // the leader has already planned - its plan is cached and every follower's shared
        // task is already resolved - but is still blocked before its own execution; a
        // follower for the same operation must not wait on that block.
        var followerResult = await executor.ExecuteAsync(operationText, cts.Token);
        var leaderStillBlocked = !leaderTask.IsCompleted;

        executionGate.Release();
        var leaderResult = await leaderTask;

        // assert
        Assert.True(leaderStillBlocked);
        Assert.Empty(followerResult.ExpectOperationResult().Errors);
        Assert.Empty(leaderResult.ExpectOperationResult().Errors);

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(1, listener.PlanStartCount(operationId));
    }

    [Fact]
    public async Task Leader_Cancellation_After_Planning_Should_Cache_Plan_Once_And_Release_Followers()
    {
        // arrange
        using var leaderCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var planningGate = new SingleFlightLeaderGate();
        var executionGate = new SingleFlightLeaderGate();
        var secondRequestObserver = new SecondRequestObserver();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => CreateSecondRequestEnteredDownstreamMiddleware(next, secondRequestObserver),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateSingleFlightLeaderBlockMiddleware(next, planningGate),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateSingleFlightLeaderBlockMiddleware(next, executionGate),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: testCts.Token);

        const string operationText =
            """
            query LeaderCancelledAfterPlanning {
              foo
            }
            """;

        // act
        var leaderTask = executor.ExecuteAsync(operationText, leaderCts.Token);
        await planningGate.WaitForEntryAsync(testCts.Token);

        var followerTask = executor.ExecuteAsync(operationText, testCts.Token);
        await secondRequestObserver.WaitForSecondRequestEnteredDownstreamAsync(testCts.Token);

        // release planning: the leader plans, caches the plan, resolves the follower's
        // task, and raises the cache event, all before it is cancelled deep in its own,
        // still-blocked, post-planning execution.
        planningGate.Release();
        var followerResult = await followerTask;

        await executionGate.WaitForEntryAsync(testCts.Token);
        await leaderCts.CancelAsync();
        var leaderResult = await leaderTask;

        // assert
        Assert.Empty(followerResult.ExpectOperationResult().Errors);
        Assert.NotEmpty(leaderResult.ExpectOperationResult().Errors);
        Assert.Contains(
            leaderResult.ExpectOperationResult().Errors,
            e => e.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase));

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(1, listener.PlanStartCount(operationId));
        Assert.Equal(1, listener.AddedToCacheCount(operationId));

        var operationPlanCache = executor.Schema.Services.GetRequiredService<Cache<OperationPlan>>();
        Assert.Equal(1, operationPlanCache.Count);
        Assert.True(operationPlanCache.TryGet(operationId, out _));
    }

    [Fact]
    public async Task Leader_Cancellation_Before_Planning_Should_Not_Be_Observed_By_Followers()
    {
        // arrange
        using var leaderCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var planningGate = new SingleFlightLeaderGate();
        var secondRequestObserver = new SecondRequestObserver();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => CreateSecondRequestEnteredDownstreamMiddleware(next, secondRequestObserver),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateLeaderPlanningBlockMiddleware(next, planningGate),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: testCts.Token);

        const string operationText =
            """
            query LeaderCancelledBeforePlanning {
              foo
            }
            """;

        // act
        var leaderTask = executor.ExecuteAsync(operationText, leaderCts.Token);
        await planningGate.WaitForEntryAsync(testCts.Token);

        var followerTask = executor.ExecuteAsync(operationText, testCts.Token);
        await secondRequestObserver.WaitForSecondRequestEnteredDownstreamAsync(testCts.Token);

        // the leader is cancelled before it plans. Releasing it afterward makes the planner
        // observe an already-cancelled token, so the leader fails without ever producing a
        // plan. The follower must not observe that cancellation - it becomes the new leader
        // candidate and plans the operation itself.
        await leaderCts.CancelAsync();
        planningGate.Release();

        var leaderResult = await leaderTask;
        var followerResult = await followerTask;

        // assert
        Assert.NotEmpty(leaderResult.ExpectOperationResult().Errors);
        Assert.Empty(followerResult.ExpectOperationResult().Errors);

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(2, listener.PlanStartCount(operationId));
        Assert.Equal(1, listener.PlanErrorCount(operationId));
        Assert.Equal(1, listener.AddedToCacheCount(operationId));
    }

    [Fact]
    public async Task Leader_ShortCircuit_Before_Planning_Should_Release_Followers()
    {
        // arrange
        using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var followerCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var leaderGate = new SingleFlightLeaderGate();
        var secondRequestObserver = new SecondRequestObserver();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateSecondRequestEnteredDownstreamMiddleware(next, secondRequestObserver),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateLeaderShortCircuitBeforePlanningMiddleware(next, leaderGate),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: testCts.Token);

        const string operationText =
            """
            query LeaderShortCircuitsBeforePlanning {
              foo
            }
            """;

        // act
        var leaderTask = executor.ExecuteAsync(operationText, testCts.Token);
        await leaderGate.WaitForEntryAsync(testCts.Token);

        var followerStopwatch = Stopwatch.StartNew();
        var followerTask = executor.ExecuteAsync(operationText, followerCts.Token);
        await secondRequestObserver.WaitForSecondRequestEnteredDownstreamAsync(testCts.Token);

        // the leader short-circuits with a result instead of calling into OperationPlanMiddleware;
        // the follower must be released by that, not by waiting out its own token.
        leaderGate.Release();
        var followerResult = await followerTask;
        followerStopwatch.Stop();
        var leaderResult = await leaderTask;

        // assert
        Assert.NotEmpty(leaderResult.ExpectOperationResult().Errors);
        Assert.NotEmpty(followerResult.ExpectOperationResult().Errors);
        Assert.True(
            followerStopwatch.Elapsed < TimeSpan.FromSeconds(2),
            "The follower should be released once the leader's task resolves, not wait out "
                + $"its own token (elapsed: {followerStopwatch.Elapsed}).");

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(0, listener.PlanStartCount(operationId));
        Assert.Equal(0, listener.AddedToCacheCount(operationId));

        // the in-flight entry was removed, so a later request for the same operation plans
        // normally instead of coalescing onto the dead entry.
        var laterResult = await executor.ExecuteAsync(operationText, testCts.Token);
        Assert.Empty(laterResult.ExpectOperationResult().Errors);
        Assert.Equal(1, listener.PlanStartCount(operationId));
    }

    [Fact]
    public async Task Leader_Cancellation_In_Middleware_Before_Planning_Should_Not_Be_Observed_By_Followers()
    {
        // arrange
        using var leaderCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var planningGate = new SingleFlightLeaderGate();
        var secondRequestObserver = new SecondRequestObserver();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateSecondRequestEnteredDownstreamMiddleware(next, secondRequestObserver),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateSingleFlightLeaderBlockMiddleware(next, planningGate),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: testCts.Token);

        const string operationText =
            """
            query LeaderCancelledInMiddlewareBeforePlanning {
              foo
            }
            """;

        // act
        var leaderTask = executor.ExecuteAsync(operationText, leaderCts.Token);
        await planningGate.WaitForEntryAsync(testCts.Token);

        var followerTask = executor.ExecuteAsync(operationText, testCts.Token);
        await secondRequestObserver.WaitForSecondRequestEnteredDownstreamAsync(testCts.Token);

        // the leader observes its own cancellation directly in the middleware, before the
        // planner ever runs: the OCE is raised right where the leader's path awaits `next`,
        // not deep inside the planner.
        await leaderCts.CancelAsync();

        var leaderResult = await leaderTask;
        planningGate.Release();
        var followerResult = await followerTask;

        // assert
        Assert.NotEmpty(leaderResult.ExpectOperationResult().Errors);
        Assert.Empty(followerResult.ExpectOperationResult().Errors);

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(1, listener.PlanStartCount(operationId));
        Assert.Equal(1, listener.AddedToCacheCount(operationId));
    }

    [Fact]
    public async Task Follower_Cancellation_Should_Not_Cancel_Leader_Planning()
    {
        // arrange
        using var leaderCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var followerCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var blockingInterceptor = new BlockingPlannerInterceptor();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .AddOperationPlannerInterceptor(_ => blockingInterceptor)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: leaderCts.Token);

        const string operationText =
            """
            query CancelFollowerOnly {
              foo
            }
            """;

        // act
        var leaderTask = Task.Run(
                () => executor.ExecuteAsync(operationText, leaderCts.Token),
                CancellationToken.None);
        Assert.True(blockingInterceptor.WaitForEntry(TimeSpan.FromSeconds(5)));

        var followerTask = Task.Run(
                () => executor.ExecuteAsync(operationText, followerCts.Token),
                CancellationToken.None);
        var followerCompletion = await Task.WhenAny(
            followerTask,
            Task.Delay(TimeSpan.FromSeconds(2), leaderCts.Token));
        blockingInterceptor.Release();
        Assert.Same(followerTask, followerCompletion);

        var followerResult = await followerTask;
        var leaderResult = await leaderTask;

        // assert
        var followerErrors = followerResult.ExpectOperationResult().Errors;
        Assert.NotEmpty(followerErrors);
        Assert.Contains(
            followerErrors,
            e => e.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(leaderResult.ExpectOperationResult().Errors);

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(1, listener.PlanStartCount(operationId));
    }

    [Fact]
    public async Task Plan_Set_By_A_Preceding_Custom_Middleware_Is_Cached_And_Releases_Followers()
    {
        // arrange
        const string schemaDocument =
            """
            type Query {
              foo: String
            }
            """;
        const string operationText =
            """
            query PlanSetByCustomMiddleware {
              foo
            }
            """;

        // A real plan produced independently, on a throwaway executor over the same
        // schema; used below to stand in for a plan that a custom middleware ahead of
        // OperationPlanMiddleware set on the context some other way than the normal
        // CreatePlan path. Its content does not otherwise matter: the plan-capturing
        // middleware below short-circuits before the plan would ever actually be
        // executed against it.
        var primedPlans = new ConcurrentBag<OperationPlan>();
        var primingExecutor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(primedPlans),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(ComposeSchemaDocument(schemaDocument))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var primingResult = await primingExecutor.ExecuteAsync(
            operationText,
            TestContext.Current.CancellationToken);
        Assert.Empty(primingResult.ExpectOperationResult().Errors);
        var externalPlan = Assert.Single(primedPlans);

        using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var leaderGate = new SingleFlightLeaderGate();
        var secondRequestObserver = new SecondRequestObserver();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateSecondRequestEnteredDownstreamMiddleware(next, secondRequestObserver),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateExternalPlanBeforePlanningMiddleware(next, leaderGate, externalPlan),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(ComposeSchemaDocument(schemaDocument))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: testCts.Token);

        // act
        var leaderTask = executor.ExecuteAsync(operationText, testCts.Token);
        await leaderGate.WaitForEntryAsync(testCts.Token);

        var followerTask = executor.ExecuteAsync(operationText, testCts.Token);
        await secondRequestObserver.WaitForSecondRequestEnteredDownstreamAsync(testCts.Token);

        // the leader never reaches OperationPlanMiddleware's own CreatePlan path: the plan
        // was already set on its context by the middleware above, which cached it and
        // released the follower at that moment. Releasing the gate only lets the leader finish.
        leaderGate.Release();
        var results = await Task.WhenAll(leaderTask, followerTask);

        // assert
        Assert.All(results, t => Assert.Empty(t.ExpectOperationResult().Errors));

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(0, listener.PlanStartCount(operationId));
        Assert.Equal(1, listener.AddedToCacheCount(operationId));

        var operationPlanCache = executor.Schema.Services.GetRequiredService<Cache<OperationPlan>>();
        Assert.Equal(1, operationPlanCache.Count);
        Assert.True(operationPlanCache.TryGet(operationId, out var cachedPlan));
        Assert.Same(externalPlan, cachedPlan);
    }

    [Fact]
    public async Task Plan_Set_By_A_Preceding_Custom_Middleware_Releases_Followers_Before_Its_Own_Downstream_Completes()
    {
        // arrange
        const string schemaDocument =
            """
            type Query {
              foo: String
            }
            """;
        const string operationText =
            """
            query PlanSetByCustomMiddlewareReleasesEarly {
              foo
            }
            """;

        // A real plan produced independently, on a throwaway executor over the same
        // schema; stands in for the plan a custom middleware ahead of OperationPlanMiddleware
        // sets on the context some other way than the normal CreatePlan path.
        var primedPlans = new ConcurrentBag<OperationPlan>();
        var primingExecutor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(primedPlans),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(ComposeSchemaDocument(schemaDocument))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var primingResult = await primingExecutor.ExecuteAsync(
            operationText,
            TestContext.Current.CancellationToken);
        Assert.Empty(primingResult.ExpectOperationResult().Errors);
        var externalPlan = Assert.Single(primedPlans);

        using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var operationIds = new ConcurrentBag<string>();
        var leaderGate = new SingleFlightLeaderGate();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateExternalPlanBeforePlanningMiddleware(next, leaderGate, externalPlan),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(ComposeSchemaDocument(schemaDocument))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: testCts.Token);

        // act
        var leaderTask = executor.ExecuteAsync(operationText, testCts.Token);

        // the leader signals gate entry only after it has already called SetOperationPlan
        // (see CreateExternalPlanBeforePlanningMiddleware), so by the time this returns the
        // plan is already cached and every coalesced follower already released with it,
        // even though the leader itself remains blocked, deep in its own downstream call,
        // well short of completing.
        await leaderGate.WaitForEntryAsync(testCts.Token);

        var followerResult = await executor.ExecuteAsync(operationText, testCts.Token);
        var leaderStillHeld = !leaderTask.IsCompleted;

        leaderGate.Release();
        var leaderResult = await leaderTask;

        // assert
        Assert.True(leaderStillHeld, "The follower must complete while the leader is still held.");
        Assert.Empty(followerResult.ExpectOperationResult().Errors);
        Assert.Empty(leaderResult.ExpectOperationResult().Errors);

        var operationId = Assert.Single(operationIds.Distinct());
        var operationPlanCache = executor.Schema.Services.GetRequiredService<Cache<OperationPlan>>();
        Assert.Equal(1, operationPlanCache.Count);
        Assert.True(operationPlanCache.TryGet(operationId, out var cachedPlan));
        Assert.Same(externalPlan, cachedPlan);
    }

    [Fact]
    public async Task Leader_Throwing_After_Setting_The_Plan_Still_Caches_It_And_Releases_Followers()
    {
        // arrange
        const string schemaDocument =
            """
            type Query {
              foo: String
            }
            """;
        const string operationText =
            """
            query LeaderThrowsAfterSettingPlan {
              foo
            }
            """;

        // A real plan produced independently, on a throwaway executor over the same
        // schema; stands in for the plan a custom middleware ahead of OperationPlanMiddleware
        // sets on the context some other way than the normal CreatePlan path.
        var primedPlans = new ConcurrentBag<OperationPlan>();
        var primingExecutor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(primedPlans),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(ComposeSchemaDocument(schemaDocument))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var primingResult = await primingExecutor.ExecuteAsync(
            operationText,
            TestContext.Current.CancellationToken);
        Assert.Empty(primingResult.ExpectOperationResult().Errors);
        var externalPlan = Assert.Single(primedPlans);

        using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var leaderGate = new SingleFlightLeaderGate();
        var secondRequestObserver = new SecondRequestObserver();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateSecondRequestEnteredDownstreamMiddleware(next, secondRequestObserver),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateExternalPlanBeforeGateThenThrowMiddleware(next, leaderGate, externalPlan),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(ComposeSchemaDocument(schemaDocument))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: testCts.Token);

        // act
        var leaderTask = executor.ExecuteAsync(operationText, testCts.Token);
        await leaderGate.WaitForEntryAsync(testCts.Token);

        var followerTask = executor.ExecuteAsync(operationText, testCts.Token);
        await secondRequestObserver.WaitForSecondRequestEnteredDownstreamAsync(testCts.Token);

        // release the leader: it sets the plan - caching it and releasing the coalesced
        // follower above - and only then throws; the follower must complete successfully
        // with that plan regardless of the leader's own subsequent failure.
        leaderGate.Release();
        var followerResult = await followerTask;
        var leaderResult = await leaderTask;

        // assert
        Assert.NotEmpty(leaderResult.ExpectOperationResult().Errors);
        Assert.Empty(followerResult.ExpectOperationResult().Errors);

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(1, listener.AddedToCacheCount(operationId));

        var operationPlanCache = executor.Schema.Services.GetRequiredService<Cache<OperationPlan>>();
        Assert.Equal(1, operationPlanCache.Count);
        Assert.True(operationPlanCache.TryGet(operationId, out var cachedPlan));
        Assert.Same(externalPlan, cachedPlan);
    }

    [Fact]
    public async Task Retrying_Follower_Reuses_A_Plan_Cached_Meanwhile_Instead_Of_Replanning()
    {
        // arrange
        const string schemaDocument =
            """
            type Query {
              foo: String
            }
            """;
        const string operationText =
            """
            query RetryReusesCachedPlan {
              foo
            }
            """;

        // A real plan for this exact operation, produced independently and up front on a
        // throwaway executor over the same schema. It stands in below for the plan that a
        // third, fully-completed request would have cached for this operation while the
        // follower was coalesced onto the leader: a genuine, concurrently overlapping
        // third request cannot plan this operation independently here, because as long as
        // the leader's in-flight entry is registered, any concurrent request for the same
        // operation coalesces onto it too, instead of planning on its own.
        var meanwhilePlans = new ConcurrentBag<OperationPlan>();
        var meanwhileExecutor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(meanwhilePlans),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(ComposeSchemaDocument(schemaDocument))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var meanwhileResult = await meanwhileExecutor.ExecuteAsync(
            operationText,
            TestContext.Current.CancellationToken);
        Assert.Empty(meanwhileResult.ExpectOperationResult().Errors);
        var meanwhilePlan = Assert.Single(meanwhilePlans);

        using var leaderCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var planningGate = new SingleFlightLeaderGate();
        var secondRequestObserver = new SecondRequestObserver();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateSecondRequestEnteredDownstreamMiddleware(next, secondRequestObserver),
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => CreateLeaderPlanningBlockMiddleware(next, planningGate),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(ComposeSchemaDocument(schemaDocument))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: testCts.Token);

        // act
        var leaderTask = executor.ExecuteAsync(operationText, leaderCts.Token);
        await planningGate.WaitForEntryAsync(testCts.Token);

        var followerTask = executor.ExecuteAsync(operationText, testCts.Token);
        await secondRequestObserver.WaitForSecondRequestEnteredDownstreamAsync(testCts.Token);

        var operationId = Assert.Single(operationIds.Distinct());

        // While the follower is still coalesced onto the (not yet cancelled) leader, the
        // plan for this operation becomes available in the cache, exactly as if a third,
        // already-completed request had planned and cached it in the meantime. Neither the
        // still-blocked leader nor the still-waiting follower has touched the cache yet.
        var operationPlanCache = executor.Schema.Services.GetRequiredService<Cache<OperationPlan>>();
        operationPlanCache.TryAdd(operationId, meanwhilePlan);

        // the leader is cancelled before it plans. Releasing it afterward makes the
        // planner observe an already-cancelled token, so the leader fails without ever
        // producing a plan. On retry, the follower must find the plan that is already
        // cached instead of becoming a new leader candidate and planning it again.
        await leaderCts.CancelAsync();
        planningGate.Release();

        var leaderResult = await leaderTask;
        var followerResult = await followerTask;

        // assert
        Assert.NotEmpty(leaderResult.ExpectOperationResult().Errors);
        Assert.Empty(followerResult.ExpectOperationResult().Errors);

        // the leader's own, doomed attempt still starts planning before it observes its
        // cancellation; what matters is that the follower's retry does not plan a second
        // time, since it finds the plan already sitting in the cache instead.
        Assert.Equal(1, listener.PlanStartCount(operationId));
        Assert.Equal(0, listener.AddedToCacheCount(operationId));

        Assert.Equal(1, operationPlanCache.Count);
        Assert.True(operationPlanCache.TryGet(operationId, out var cachedPlan));
        Assert.Same(meanwhilePlan, cachedPlan);
    }

    [Fact]
    public async Task Faulty_AddedToCache_Listener_Does_Not_Leak_The_InFlight_Entry()
    {
        // arrange
        const string schemaDocument =
            """
            type Query {
              foo: String
            }
            """;
        const string operationText =
            """
            query FaultyListenerLeaderPlan {
              foo
            }
            """;
        // The plan cache is a fixed-size ring buffer (16 is the minimum); once every slot
        // is occupied, inserting one more distinct entry evicts whichever slot the clock
        // hand lands on next, which - since nothing above ever looks the leader's plan
        // back up - is deterministically the leader's own entry once exactly that many
        // brand new operations have been planned and cached after it.
        const int planCacheCapacity = 16;

        // A real plan produced independently, on a throwaway executor over the same
        // schema; stands in for a plan that a preceding custom middleware set on the
        // context some other way than the normal CreatePlan path (the same fallback
        // scenario covered above), this time paired with a diagnostic listener that
        // throws once the plan reaches the cache.
        var primedPlans = new ConcurrentBag<OperationPlan>();
        var primingExecutor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(primedPlans),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(ComposeSchemaDocument(schemaDocument))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var primingResult = await primingExecutor.ExecuteAsync(
            operationText,
            TestContext.Current.CancellationToken);
        Assert.Empty(primingResult.ExpectOperationResult().Errors);
        var externalPlan = Assert.Single(primedPlans);

        using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var listener = new PlanningCountDiagnosticListener();
        var faultyListener = new FaultyOnceAddedToCacheDiagnosticListener();
        var leaderOperationIds = new ConcurrentBag<string>();
        var planOnceGate = new OnceGate();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .ModifyOptions(o => o.OperationExecutionPlanCacheSize = planCacheCapacity)
            .AddDiagnosticEventListener(_ => listener)
            .AddDiagnosticEventListener(_ => faultyListener)
            .UseRequest(
                (_, next) => CreateSetPlanOnFirstRequestMiddleware(
                    next, planOnceGate, externalPlan, leaderOperationIds),
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => CreatePlanCaptureMiddleware(),
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(ComposeSchemaDocument(schemaDocument))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: testCts.Token);

        // act
        // the leader's plan is set by the custom middleware, so OperationPlanMiddleware
        // never plans it itself; OperationPlanCacheMiddleware's finally caches it and the
        // faulty listener then throws, before it can TrySetResult. With the fix, the
        // in-flight entry is still evicted; without it, the entry is leaked and every
        // later request for this operation would coalesce onto its never-completing task
        // instead of being served from the cache or planning again. The plan-capturing
        // middleware above short-circuits before any plan would actually be executed, the
        // same way the fallback-caching test above does, since the externally-set plan
        // was produced on a different executor.
        var leaderResult = await executor.ExecuteAsync(operationText, testCts.Token);

        // evict the leader's cached plan by planning and caching one brand new operation
        // per remaining slot, then one more: the cache never looks the leader's plan back
        // up in the meantime, so once every slot has been visited once (clearing its
        // "recently used" bit) the next new entry deterministically evicts it, forcing
        // the final request below to actually consult the in-flight map instead of
        // short-circuiting on the cache.
        for (var i = 0; i < planCacheCapacity; i++)
        {
            var fillerOperationText =
                $$"""
                query FaultyListenerCacheFiller{{i}} {
                  foo
                }
                """;
            var fillerResult = await executor.ExecuteAsync(fillerOperationText, testCts.Token);
            Assert.Empty(fillerResult.ExpectOperationResult().Errors);
        }

        var operationPlanCache = executor.Schema.Services.GetRequiredService<Cache<OperationPlan>>();
        var operationId = Assert.Single(leaderOperationIds.Distinct());
        Assert.False(
            operationPlanCache.TryGet(operationId, out _),
            "The leader's plan should have been evicted by the filler operations above.");

        // without the fix, the leader's finally never removed its in-flight entry, so this
        // request coalesces onto its never-completing task and hangs until its own
        // cancellation instead of planning again.
        var secondRequestTask = executor.ExecuteAsync(operationText, testCts.Token);
        var firstToComplete = await Task.WhenAny(
            secondRequestTask,
            Task.Delay(TimeSpan.FromSeconds(2), testCts.Token));

        // assert
        Assert.NotEmpty(leaderResult.ExpectOperationResult().Errors);
        Assert.Same(
            secondRequestTask,
            firstToComplete);
        var secondResult = await secondRequestTask;
        Assert.Empty(secondResult.ExpectOperationResult().Errors);

        Assert.Equal(1, listener.PlanStartCount(operationId));
        Assert.Equal(planCacheCapacity, operationPlanCache.Count);
        Assert.True(operationPlanCache.TryGet(operationId, out _));
    }

    private static RequestDelegate CreateExternalPlanBeforePlanningMiddleware(
        RequestDelegate next,
        SingleFlightLeaderGate gate,
        OperationPlan plan)
        => async context =>
        {
            // Only the leader candidate lacks a plan at this point; a follower already
            // carries the coalesced plan set by OperationPlanCacheMiddleware.
            if (context.GetOperationPlan() is null
                && context.Features.Get<TaskCompletionSource<OperationPlan>>() is not null)
            {
                // Simulate a plan produced by a preceding custom middleware, entirely
                // outside OperationPlanMiddleware's own CreatePlan path.
                context.SetOperationPlan(plan);
                gate.SignalEntry();
                await gate.WaitForReleaseAsync(context.RequestAborted);
            }

            await next(context);
        };

    private static RequestDelegate CreateExternalPlanBeforeGateThenThrowMiddleware(
        RequestDelegate next,
        SingleFlightLeaderGate gate,
        OperationPlan plan)
        => async context =>
        {
            // Only the leader candidate lacks a plan at this point; a follower already
            // carries the coalesced plan set by OperationPlanCacheMiddleware.
            if (context.GetOperationPlan() is null
                && context.Features.Get<TaskCompletionSource<OperationPlan>>() is not null)
            {
                gate.SignalEntry();
                await gate.WaitForReleaseAsync(context.RequestAborted);

                // Simulate a plan produced by a preceding custom middleware, entirely
                // outside OperationPlanMiddleware's own CreatePlan path, followed by a
                // failure in that same middleware after the plan has already been set.
                context.SetOperationPlan(plan);
                throw new InvalidOperationException("Boom: the leader throws after setting the plan.");
            }

            await next(context);
        };

    private static RequestDelegate CreateSetPlanOnFirstRequestMiddleware(
        RequestDelegate next,
        OnceGate gate,
        OperationPlan plan,
        ConcurrentBag<string> leaderOperationIds)
        => context =>
        {
            if (gate.TryEnter())
            {
                leaderOperationIds.Add(context.GetOperationId());
                context.SetOperationPlan(plan);
            }

            return next(context);
        };

    private static RequestDelegate CreateGateMiddleware(
        RequestDelegate next,
        RequestGate gate)
        => async context =>
        {
            await gate.SignalAndWaitAsync(context.RequestAborted);
            await next(context);
        };

    private static RequestDelegate CreateOperationIdCaptureMiddleware(
        RequestDelegate next,
        ConcurrentBag<string> operationIds)
        => async context =>
        {
            operationIds.Add(context.GetOperationId());
            await next(context);
        };

    private static RequestDelegate CreateSingleFlightLeaderDelayMiddleware(
        RequestDelegate next,
        TimeSpan delay)
        => async context =>
        {
            if (context.Features.Get<TaskCompletionSource<OperationPlan>>() is not null)
            {
                await Task.Delay(delay, context.RequestAborted);
            }

            await next(context);
        };

    private static RequestDelegate CreateSingleFlightLeaderBlockMiddleware(
        RequestDelegate next,
        SingleFlightLeaderGate gate)
        => async context =>
        {
            if (context.Features.Get<TaskCompletionSource<OperationPlan>>() is not null)
            {
                gate.SignalEntry();
                await gate.WaitForReleaseAsync(context.RequestAborted);
            }

            await next(context);
        };

    private static RequestDelegate CreateLeaderPlanningBlockMiddleware(
        RequestDelegate next,
        SingleFlightLeaderGate gate)
        => async context =>
        {
            if (context.Features.Get<TaskCompletionSource<OperationPlan>>() is not null)
            {
                gate.SignalEntry();

                // Deliberately does not observe this request's own cancellation: the test
                // cancels the leader's token before releasing this gate, so the leader's
                // cancellation is instead observed by the planner itself, right where the
                // production TCS-resolution logic lives.
                await gate.WaitForReleaseAsync(CancellationToken.None);
            }

            await next(context);
        };

    private static RequestDelegate CreateLeaderShortCircuitBeforePlanningMiddleware(
        RequestDelegate next,
        SingleFlightLeaderGate gate)
    {
        var shortCircuited = 0;

        return async context =>
        {
            // Only the original leader short-circuits; a later request that becomes the new
            // leader after the in-flight entry was removed must plan normally.
            if (context.Features.Get<TaskCompletionSource<OperationPlan>>() is not null
                && Interlocked.Exchange(ref shortCircuited, 1) == 0)
            {
                gate.SignalEntry();
                await gate.WaitForReleaseAsync(context.RequestAborted);
                context.Result = OperationResult.FromError(
                    new Error { Message = "Leader short-circuited before planning." });
                return;
            }

            await next(context);
        };
    }

    private static RequestDelegate CreateSecondRequestEnteredDownstreamMiddleware(
        RequestDelegate next,
        SecondRequestObserver observer)
        => async context =>
        {
            if (!observer.IsSecondRequest())
            {
                await next(context);
                return;
            }

            ValueTask execution;

            try
            {
                execution = next(context);
            }
            finally
            {
                observer.SignalSecondRequestEnteredDownstream();
            }

            await execution;
        };

    private static RequestDelegate CreatePlanCaptureMiddleware(
        ConcurrentBag<OperationPlan>? plans = null)
        => context =>
        {
            if (context.GetOperationPlan() is { } plan)
            {
                plans?.Add(plan);
            }

            context.Result =
                new OperationResult(
                    ImmutableOrderedDictionary<string, object?>.Empty.Add("operationPlan", context.GetOperationPlan()));
            return ValueTask.CompletedTask;
        };

    private static IOperationRequest CreateVariableCostRequest(int n)
        => OperationRequestBuilder.New()
            .SetDocument(VariableCostOperation)
            .SetVariableValues(new Dictionary<string, object?> { ["n"] = n })
            .Build();

    private sealed class RequestGate(int expectedRequests)
    {
        private readonly TaskCompletionSource _allArrived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrived;

        public ValueTask SignalAndWaitAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _arrived) >= expectedRequests)
            {
                _allArrived.TrySetResult();
            }

            return new ValueTask(_allArrived.Task.WaitAsync(cancellationToken));
        }
    }

    private sealed class BlockingPlannerInterceptor : IOperationPlannerInterceptor
    {
        private readonly ManualResetEventSlim _entered = new(false);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool WaitForEntry(TimeSpan timeout)
            => _entered.Wait(timeout);

        public void Release()
            => _release.TrySetResult();

        public void OnAfterPlanCompleted(
            OperationDocumentInfo operationDocumentInfo,
            OperationPlan operationPlan)
        {
            _entered.Set();
            _release.Task.GetAwaiter().GetResult();
        }
    }

    private sealed class SingleFlightLeaderGate
    {
        private readonly TaskCompletionSource _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _entryCount;

        public int EntryCount => Volatile.Read(ref _entryCount);

        public void SignalEntry()
        {
            Interlocked.Increment(ref _entryCount);
            _entered.TrySetResult();
        }

        public void Release()
            => _release.TrySetResult();

        public ValueTask WaitForEntryAsync(CancellationToken cancellationToken)
            => new(_entered.Task.WaitAsync(cancellationToken));

        public ValueTask WaitForReleaseAsync(CancellationToken cancellationToken)
            => new(_release.Task.WaitAsync(cancellationToken));
    }

    private sealed class OnceGate
    {
        private int _entered;

        public bool TryEnter()
            => Interlocked.Exchange(ref _entered, 1) == 0;
    }

    private sealed class FaultyOnceAddedToCacheDiagnosticListener : FusionExecutionDiagnosticEventListener
    {
        private int _hasThrown;

        public override void AddedOperationPlanToCache(RequestContext context, string operationPlanId)
        {
            if (Interlocked.Exchange(ref _hasThrown, 1) == 0)
            {
                throw new InvalidOperationException(
                    "Boom: a faulty diagnostic listener throwing from AddedOperationPlanToCache.");
            }
        }
    }

    private sealed class SecondRequestObserver
    {
        private readonly TaskCompletionSource _secondRequestEnteredDownstream =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _requestCount;

        public bool IsSecondRequest()
            => Interlocked.Increment(ref _requestCount) == 2;

        public void SignalSecondRequestEnteredDownstream()
            => _secondRequestEnteredDownstream.TrySetResult();

        public ValueTask WaitForSecondRequestEnteredDownstreamAsync(CancellationToken cancellationToken)
            => new(_secondRequestEnteredDownstream.Task.WaitAsync(cancellationToken));
    }

    private sealed class PlanningCountDiagnosticListener : FusionExecutionDiagnosticEventListener
    {
        private readonly ConcurrentDictionary<string, int> _planStarts = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, int> _planErrors = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, int> _addedToCache = new(StringComparer.Ordinal);

        public override IDisposable PlanOperation(RequestContext context, string operationPlanId)
        {
            _planStarts.AddOrUpdate(operationPlanId, 1, static (_, count) => count + 1);
            return EmptyScope;
        }

        public override void PlanOperationError(
            RequestContext context,
            string operationId,
            Exception error)
        {
            _planErrors.AddOrUpdate(operationId, 1, static (_, count) => count + 1);
        }

        public override void AddedOperationPlanToCache(RequestContext context, string operationPlanId)
        {
            _addedToCache.AddOrUpdate(operationPlanId, 1, static (_, count) => count + 1);
        }

        public int PlanStartCount(string operationId)
            => _planStarts.TryGetValue(operationId, out var count)
                ? count
                : 0;

        public int PlanErrorCount(string operationId)
            => _planErrors.TryGetValue(operationId, out var count)
                ? count
                : 0;

        public int AddedToCacheCount(string operationId)
            => _addedToCache.TryGetValue(operationId, out var count)
                ? count
                : 0;
    }

    private sealed class CountingOperationDocumentNormalizer(IOperationDocumentNormalizer inner, Action onNormalize)
        : IOperationDocumentNormalizer
    {
        public DocumentNode NormalizeDocument(RequestContext context)
        {
            onNormalize();
            return inner.NormalizeDocument(context);
        }
    }
}
