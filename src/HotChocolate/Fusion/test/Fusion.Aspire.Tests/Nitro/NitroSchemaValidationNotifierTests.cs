using System.Collections.Concurrent;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroSchemaValidationNotifierTests
{
    [Theory]
    [MemberData(nameof(NotificationTransitionData))]
    public async Task Enqueue_Should_FollowNotificationTransitionTable_When_ReportChanges(
        string sequence,
        string expected)
    {
        // arrange
        using var stopping = new CancellationTokenSource();
        var notifier = new RecordingNotifier();
        var reports = CreateReports(sequence);
        var expectedNotificationCount = expected == "<none>"
            ? 0
            : expected.Split('\n').Length;
        var worker = CreateWorker(
            (_, _) => Task.FromResult(reports.Dequeue()),
            notifier,
            stopping.Token);

        // act
        for (var index = 0; index < sequence.Split(',').Length; index++)
        {
            var requestId = $"request-{index}";
            worker.Enqueue(
                new GatewaySchemaValidationRequest(
                    [(byte)index],
                    $"schema-{index}"));
            await WaitForReportAsync(worker, requestId);
        }

        if (expectedNotificationCount > 0)
        {
            await WaitForAsync(
                () => notifier.Notifications.Count == expectedNotificationCount);
        }

        stopping.Cancel();

        // assert
        Assert.Equal(expected, DescribeIntents(notifier.Notifications));
    }

    [Fact]
    public async Task Enqueue_Should_UseGatewayAndStageWithoutDetails_When_ViolationsAreReported()
    {
        // arrange
        using var stopping = new CancellationTokenSource();
        var notifier = new RecordingNotifier();
        var worker = CreateWorker(
            (_, _) => Task.FromResult(DetailedViolations()),
            notifier,
            stopping.Token,
            gatewayName: "orders-gateway",
            stage: "staging");

        // act
        worker.Enqueue(new GatewaySchemaValidationRequest([1], "schema"));
        await WaitForReportAsync(worker, "request-details");
        await WaitForAsync(() => notifier.Notifications.Count == 1);
        stopping.Cancel();

        // assert
        notifier.Notifications
            .Select(
                notification =>
                    notification.Message is null
                        ? notification.Intent
                        : $"{notification.Intent}: {notification.Message}")
            .MatchInlineSnapshots(
            [
                "Error: Detected breaking schema changes in 'orders-gateway' against stage "
                    + "'staging'; check the logs for details."
            ]);
    }

    [Fact]
    public void Notify_Should_UseExpectedTitleAndIntent_When_InteractionIsAvailable()
    {
        // arrange
        var interactionService = CreateInteractionService(out var proxy);
        var notifier = new NitroSchemaValidationNotifier(
            interactionService,
            new TestHostApplicationLifetime(),
            NullLogger<NitroSchemaValidationNotifier>.Instance);

        // act
        notifier.NotifyViolations("gateway", "Breaking schema changes were detected.");
        notifier.NotifyRestored("Client contracts restored.");

        // assert
        proxy.Prompts
            .Select(prompt => prompt.ToString())
            .MatchInlineSnapshots(
            [
                "Error | Nitro: | Breaking schema changes were detected. | View logs | "
                    + "/consolelogs/resource/gateway",
                "Success | Nitro: | Client contracts restored. | <none> | <none>"
            ]);
    }

    [Fact]
    public void NotifyViolations_Should_SkipPromptWithoutThrowing_When_InteractionsAreUnavailable()
    {
        // arrange
        var interactionService = CreateInteractionService(out var proxy);
        proxy.IsAvailable = false;
        var notifier = new NitroSchemaValidationNotifier(
            interactionService,
            new TestHostApplicationLifetime(),
            NullLogger<NitroSchemaValidationNotifier>.Instance);

        // act
        var exception = Record.Exception(
            () => notifier.NotifyViolations("gateway", "Breaking schema changes were detected."));

        // assert
        $"""
        Exception: {exception?.GetType().Name ?? "<none>"}
        Prompt calls: {proxy.Prompts.Count}
        """.MatchInlineSnapshot(
            """
            Exception: <none>
            Prompt calls: 0
            """);
    }

    [Fact]
    public async Task NotifyViolations_Should_ReturnImmediatelyAndObserveFailure_When_PromptIsPending()
    {
        // arrange
        var interactionService = CreateInteractionService(out var proxy);
        var prompt = new TaskCompletionSource<InteractionResult<bool>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        proxy.PromptTask = prompt.Task;
        var logger = new RecordingLogger<NitroSchemaValidationNotifier>();
        var notifier = new NitroSchemaValidationNotifier(
            interactionService,
            new TestHostApplicationLifetime(),
            logger);

        // act
        var exception = Record.Exception(
            () => notifier.NotifyViolations("gateway", "Breaking schema changes were detected."));
        var wasPendingAfterNotifyReturned = !prompt.Task.IsCompleted;
        prompt.SetException(new InvalidOperationException("dashboard disconnected"));
        await WaitForAsync(() => logger.Entries.Count == 1);

        // assert
        $"""
        Exception: {exception?.GetType().Name ?? "<none>"}
        Pending after return: {wasPendingAfterNotifyReturned}
        Prompt calls: {proxy.Prompts.Count}
        Log level: {logger.Entries[0].Level}
        Log: {logger.Entries[0].Message}
        Logged exception: {logger.Entries[0].Exception?.Message}
        """.MatchInlineSnapshot(
            """
            Exception: <none>
            Pending after return: True
            Prompt calls: 1
            Log level: Debug
            Log: The Nitro notification could not be shown.
            Logged exception: dashboard disconnected
            """);
    }

    public static TheoryData<string, string> NotificationTransitionData()
        => new()
        {
            { "V1", "Error" },
            { "P,V1", "Error" },
            { "U,V1", "Error" },
            { "V1,V2", "Error\nError" },
            { "V1,V1", "Error" },
            { "V1,P", "Error\nSuccess" },
            { "P", "<none>" },
            { "P,P", "<none>" },
            { "U,P", "<none>" },
            { "V1,U,P", "Error\nSuccess" },
            { "V1,U,V1", "Error\nError" },
            { "U", "<none>" },
            { "P,U", "<none>" },
            { "V1,U", "Error" },
            { "U,U", "<none>" }
        };

    private static Queue<NitroSchemaValidationReport> CreateReports(string sequence)
    {
        var reports = sequence
            .Split(',')
            .Select(
                (status, index) =>
                    status switch
                    {
                        "P" => Passed(index),
                        "U" => Unavailable(index),
                        "V1" => Violations(index, "HC001"),
                        "V2" => Violations(index, "HC002"),
                        _ => throw new InvalidOperationException(
                            $"Unknown validation status '{status}'.")
                    });

        return new Queue<NitroSchemaValidationReport>(reports);
    }

    private static GatewaySchemaValidationWorker CreateWorker(
        Func<GatewaySchemaValidationRequest, CancellationToken, Task<NitroSchemaValidationReport>>
            validate,
        INitroSchemaValidationNotifier notifier,
        CancellationToken stoppingToken,
        string gatewayName = "gateway",
        string stage = "production")
        => new(
            gatewayName,
            stage,
            validate,
            NullLogger.Instance,
            notifier,
            stoppingToken,
            NullLogger<GatewaySchemaValidationWorker>.Instance);

    private static NitroSchemaValidationReport Passed(int index)
        => NitroSchemaValidationReport.Passed(
            $"schema-{index}",
            $"request-{index}",
            DateTimeOffset.UtcNow);

    private static NitroSchemaValidationReport Unavailable(int index)
        => NitroSchemaValidationReport.Unavailable(
            $"schema-{index}",
            "network",
            DateTimeOffset.UtcNow,
            $"request-{index}");

    private static NitroSchemaValidationReport Violations(int index, string code)
        => new(
            NitroSchemaValidationStatus.Violations,
            $"schema-{index}",
            $"request-{index}",
            [
                Client(
                    "client",
                    Operation(
                        "operation",
                        new NitroSchemaValidationFinding(
                            "Client contract violations",
                            "PersistedQueryValidationError",
                            "Field does not exist.",
                            code,
                            Path: "query.field",
                            Line: 1,
                            Column: 2)))
            ],
            [],
            null,
            DateTimeOffset.UtcNow);

    private static NitroSchemaValidationReport DetailedViolations()
        => new(
            NitroSchemaValidationStatus.Violations,
            "schema",
            "request-details",
            [
                Client(
                    "client-1",
                    Operation(
                        "operation-1",
                        SecretFinding()),
                    Operation(
                        "operation-2",
                        SecretFinding())),
                Client(
                    "client-2",
                    Operation(
                        "operation-3",
                        SecretFinding()))
            ],
            [],
            null,
            DateTimeOffset.UtcNow);

    private static NitroClientContractViolation Client(
        string clientId,
        params NitroOperationContractViolation[] operations)
        => new(clientId, $"Name for {clientId}", operations);

    private static NitroOperationContractViolation Operation(
        string hash,
        NitroSchemaValidationFinding finding)
        => new(hash, ["production"], [finding]);

    private static NitroSchemaValidationFinding SecretFinding()
        => new(
            "Client contract violations",
            "PersistedQueryValidationError",
            "token=super-secret; type Query { secret: String }",
            "HC-SECRET",
            Path: "query.secret",
            Line: 1,
            Column: 2);

    private static async Task WaitForReportAsync(
        GatewaySchemaValidationWorker worker,
        string requestId)
        => await WaitForAsync(
            () => string.Equals(
                worker.LatestReport?.RequestId,
                requestId,
                StringComparison.Ordinal));

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private static string DescribeIntents(
        IReadOnlyList<RecordedNotification> notifications)
        => notifications.Count == 0
            ? "<none>"
            : string.Join('\n', notifications.Select(notification => notification.Intent));

    private static IInteractionService CreateInteractionService(
        out RecordingInteractionService proxy)
    {
        proxy = new RecordingInteractionService();
        return proxy;
    }

    private sealed class RecordingNotifier : INitroSchemaValidationNotifier
    {
        private readonly ConcurrentQueue<RecordedNotification> _notifications = [];

        public IReadOnlyList<RecordedNotification> Notifications => [.. _notifications];

        public void NotifyViolations(string gatewayName, string message)
            => _notifications.Enqueue(new("Error", message));

        public void NotifyRestored(string message)
            => _notifications.Enqueue(new("Success", message));
    }

    private sealed class RecordingInteractionService : IInteractionService
    {
        private readonly ConcurrentQueue<RecordedPrompt> _prompts = [];

        public bool IsAvailable { get; set; } = true;

        public Task<InteractionResult<bool>>? PromptTask { get; set; }

        public IReadOnlyList<RecordedPrompt> Prompts => [.. _prompts];

        public Task<InteractionResult<bool>> PromptNotificationAsync(
            string title,
            string message,
            NotificationInteractionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            _prompts.Enqueue(
                new RecordedPrompt(
                    title,
                    message,
                    options?.Intent ?? MessageIntent.None,
                    options?.LinkText,
                    options?.LinkUrl));

            return PromptTask ?? Task.FromResult(InteractionResult.Ok(true));
        }

        public Task<InteractionResult<bool>> PromptConfirmationAsync(
            string title,
            string message,
            MessageBoxInteractionOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw UnexpectedCall(nameof(PromptConfirmationAsync));

        public Task<InteractionResult<bool>> PromptMessageBoxAsync(
            string title,
            string message,
            MessageBoxInteractionOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw UnexpectedCall(nameof(PromptMessageBoxAsync));

        public Task<InteractionResult<InteractionInput>> PromptInputAsync(
            string title,
            string? message,
            string inputLabel,
            string placeHolder,
            InputsDialogInteractionOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw UnexpectedCall(nameof(PromptInputAsync));

        public Task<InteractionResult<InteractionInput>> PromptInputAsync(
            string title,
            string? message,
            InteractionInput input,
            InputsDialogInteractionOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw UnexpectedCall(nameof(PromptInputAsync));

        public Task<InteractionResult<InteractionInputCollection>> PromptInputsAsync(
            string title,
            string? message,
            IReadOnlyList<InteractionInput> inputs,
            InputsDialogInteractionOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw UnexpectedCall(nameof(PromptInputsAsync));

        private static InvalidOperationException UnexpectedCall(string method)
            => new($"Unexpected interaction call '{method}'.");
    }

    private sealed record RecordedNotification(string Intent, string? Message = null);

    private sealed record RecordedPrompt(
        string Title,
        string Message,
        MessageIntent Intent,
        string? LinkText,
        string? LinkUrl)
    {
        public override string ToString()
            => $"{Intent} | {Title} | {Message} | {LinkText ?? "<none>"} | {LinkUrl ?? "<none>"}";
    }
}
