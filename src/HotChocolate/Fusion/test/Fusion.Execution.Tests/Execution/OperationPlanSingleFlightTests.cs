using System.Collections.Concurrent;
using System.Diagnostics;
using HotChocolate.Caching.Memory;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using Microsoft.Extensions.DependencyInjection;

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

        // release planning: the leader plans, caches the plan, raises the cache event, and
        // resolves the follower's task, all before it is cancelled deep in its own,
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
        // was already set on its context by the middleware above. Releasing it lets the
        // pipeline return, which must cache that plan and release the follower with it.
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
}
