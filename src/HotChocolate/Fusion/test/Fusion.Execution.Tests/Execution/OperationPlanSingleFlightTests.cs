using System.Collections.Concurrent;
using System.Diagnostics;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;
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
    public async Task Follower_And_Cache_Hit_Never_Rewrite_Leader_Rewrites_Once_On_Cold_Id()
    {
        // arrange
        // Coercion calls the normalizer for every request; compare cached documents to count only actual rewrites.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var operationIds = new ConcurrentBag<string>();
        var leaderGate = new SingleFlightLeaderGate();
        var secondRequestObserver = new SecondRequestObserver();
        var rewriteCount = 0;

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .ConfigureSchemaServices((_, schemaServices) =>
            {
                schemaServices.RemoveAll<IOperationDocumentNormalizer>();
                schemaServices.AddSingleton<IOperationDocumentNormalizer>(
                    sp => new RewriteCountingOperationDocumentNormalizer(
                        new OperationDocumentNormalizer(
                            sp.GetRequiredService<FusionSchemaDefinition>(),
                            sp.GetRequiredService<NormalizedDocumentCache>()),
                        sp.GetRequiredService<NormalizedDocumentCache>(),
                        () => Interlocked.Increment(ref rewriteCount)));
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
            query FollowerAndCacheHitNeverRewrite {
              foo
            }
            """;

        // act
        // Wait until the leader has warmed the normalized-document cache before starting the follower.
        var leaderTask = executor.ExecuteAsync(operationText, cts.Token);
        await leaderGate.WaitForEntryAsync(cts.Token);

        var followerTask = executor.ExecuteAsync(operationText, cts.Token);
        await secondRequestObserver.WaitForSecondRequestEnteredDownstreamAsync(cts.Token);

        leaderGate.Release();
        var results = await Task.WhenAll(leaderTask, followerTask);

        // assert
        Assert.All(results, t => Assert.Empty(t.ExpectOperationResult().Errors));
        Assert.Single(operationIds.Distinct());

        Assert.Equal(1, Volatile.Read(ref rewriteCount));

        var cachedResult = await executor.ExecuteAsync(operationText, cts.Token);
        Assert.Empty(cachedResult.ExpectOperationResult().Errors);
        Assert.Equal(1, Volatile.Read(ref rewriteCount));
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
            .AddInMemoryConfiguration(ComposeSchemaDocument(defaultListSize: 1, VariableCostSchema))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);
        using var costlyRequest = CreateVariableCostRequest(1000);

        // act
        var rejectedResults = await Task.WhenAll(
            Enumerable.Range(0, rejectedRequestCount)
                .Select(_ => executor.ExecuteAsync(costlyRequest, cts.Token)));

        using var affordableRequest = CreateVariableCostRequest(1);
        var affordableResult = await executor.ExecuteAsync(affordableRequest, cts.Token);

        // assert
        Assert.All(rejectedResults, t => Assert.NotEmpty(t.ExpectOperationResult().Errors));
        Assert.Empty(affordableResult.ExpectOperationResult().Errors);

        var operationId = Assert.Single(operationIds);
        Assert.Equal(1, listener.PlanStartCount(operationId));

        var operationPlanCache = executor.Schema.Services.GetRequiredService<OperationPlanCache>();
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

        // The leader has published its plan but is blocked before execution.
        // The follower should finish without waiting for that block.
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

        // Allow planning to finish before cancelling the leader during execution.
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

        var operationPlanCache = executor.Schema.Services.GetRequiredService<OperationPlanCache>();
        Assert.Equal(1, operationPlanCache.Count);
        Assert.True(operationPlanCache.TryGetPlan(operationId, out _));
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

        // Cancel before planning so the follower must take over without a completed plan.
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

        // Let the leader return without a plan to test that the follower is released.
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

        // A later request must be able to plan after the failed entry is removed.
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

        // Cancel while the leader is in middleware, before the planner runs.
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

        // Prepare a plan for custom middleware to supply before the normal planning stage.
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

        // The custom middleware has already supplied the plan; release the leader to finish.
        leaderGate.Release();
        var results = await Task.WhenAll(leaderTask, followerTask);

        // assert
        Assert.All(results, t => Assert.Empty(t.ExpectOperationResult().Errors));

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(0, listener.PlanStartCount(operationId));
        Assert.Equal(1, listener.AddedToCacheCount(operationId));

        var operationPlanCache = executor.Schema.Services.GetRequiredService<OperationPlanCache>();
        Assert.Equal(1, operationPlanCache.Count);
        Assert.True(operationPlanCache.TryGetPlan(operationId, out var cachedPlan));
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

        // Prepare a plan for custom middleware to supply before the normal planning stage.
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

        // The gate opens after SetOperationPlan, while the leader remains blocked in downstream middleware.
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
        var operationPlanCache = executor.Schema.Services.GetRequiredService<OperationPlanCache>();
        Assert.Equal(1, operationPlanCache.Count);
        Assert.True(operationPlanCache.TryGetPlan(operationId, out var cachedPlan));
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

        // Prepare a plan for custom middleware to supply before the normal planning stage.
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

        // The leader publishes the plan and then throws; the follower must still receive the plan.
        leaderGate.Release();
        var followerResult = await followerTask;
        var leaderResult = await leaderTask;

        // assert
        Assert.NotEmpty(leaderResult.ExpectOperationResult().Errors);
        Assert.Empty(followerResult.ExpectOperationResult().Errors);

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(1, listener.AddedToCacheCount(operationId));

        var operationPlanCache = executor.Schema.Services.GetRequiredService<OperationPlanCache>();
        Assert.Equal(1, operationPlanCache.Count);
        Assert.True(operationPlanCache.TryGetPlan(operationId, out var cachedPlan));
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

        // Prepare a plan independently to simulate a cache entry appearing while the follower waits.
        // Another request on the same executor would join the existing leader.
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

        // Populate the cache while the follower is still waiting on the leader.
        var operationPlanCache = executor.Schema.Services.GetRequiredService<OperationPlanCache>();
        operationPlanCache.TryAddPlan(operationId, meanwhilePlan);

        // Cancel before planning so the follower must retry and discover the cached plan.
        await leaderCts.CancelAsync();
        planningGate.Release();

        var leaderResult = await leaderTask;
        var followerResult = await followerTask;

        // assert
        Assert.NotEmpty(leaderResult.ExpectOperationResult().Errors);
        Assert.Empty(followerResult.ExpectOperationResult().Errors);

        // The cancelled leader starts planning once; the follower's retry should use the cache.
        Assert.Equal(1, listener.PlanStartCount(operationId));
        Assert.Equal(0, listener.AddedToCacheCount(operationId));

        Assert.Equal(1, operationPlanCache.Count);
        Assert.True(operationPlanCache.TryGetPlan(operationId, out var cachedPlan));
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
        // Sixteen is the minimum capacity; enough distinct entries will evict the leader's unused plan.
        const int planCacheCapacity = 16;

        // Prepare a plan for custom middleware to supply with a cache listener that throws.
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
        // The diagnostic listener throws after the custom middleware publishes the plan.
        var leaderResult = await executor.ExecuteAsync(operationText, testCts.Token);

        // Evict the cached plan so the next request must consult the in-flight map.
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

        var operationPlanCache = executor.Schema.Services.GetRequiredService<OperationPlanCache>();
        var operationId = Assert.Single(leaderOperationIds.Distinct());
        Assert.False(
            operationPlanCache.TryGetPlan(operationId, out _),
            "The leader's plan should have been evicted by the filler operations above.");

        // A leaked in-flight entry would leave this request waiting until cancellation.
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
        Assert.True(operationPlanCache.TryGetPlan(operationId, out _));
    }

    private static RequestDelegate CreateExternalPlanBeforePlanningMiddleware(
        RequestDelegate next,
        SingleFlightLeaderGate gate,
        OperationPlan plan)
        => async context =>
        {
            // Followers already have a plan at this stage; only the leader needs one.
            if (context.GetOperationPlan() is null
                && context.Features.Get<TaskCompletionSource<OperationPlan>>() is not null)
            {
                // Supply a plan before the normal planning middleware.
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
            // Followers already have a plan at this stage; only the leader needs one.
            if (context.GetOperationPlan() is null
                && context.Features.Get<TaskCompletionSource<OperationPlan>>() is not null)
            {
                gate.SignalEntry();
                await gate.WaitForReleaseAsync(context.RequestAborted);

                // Fail after publishing the plan to test that followers retain the result.
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

                // Ignore cancellation at this gate so the planner observes the cancelled token.
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
            // Only the first leader short-circuits; a replacement must reach planning.
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

    private sealed class RewriteCountingOperationDocumentNormalizer(
        IOperationDocumentNormalizer inner,
        NormalizedDocumentCache normalizedDocumentCache,
        Action onRewrite)
        : IOperationDocumentNormalizer
    {
        public DocumentNode NormalizeDocument(RequestContext context)
        {
            var operationId = context.GetOperationId();
            var hadCachedDocument = normalizedDocumentCache.TryGet(operationId, out var documentCachedBefore);

            var normalizedDocument = inner.NormalizeDocument(context);

            if (!hadCachedDocument || !ReferenceEquals(documentCachedBefore, normalizedDocument))
            {
                onRewrite();
            }

            return normalizedDocument;
        }
    }
}
