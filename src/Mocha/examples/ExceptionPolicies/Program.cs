using ExceptionPolicies.Exceptions;
using ExceptionPolicies.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha;
using Mocha.Transport.InMemory;

// ---------------------------------------------------------------------------
//  Exception Policies Demo
//
//  Demonstrates all per-exception policy configurations available in Mocha.
//  Uses the InMemory transport for simplicity  no external dependencies.
// ---------------------------------------------------------------------------

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMessageBus()

    // -----------------------------------------------------------------------
    //  Exception Policies  the main showcase
    //
    //  Per-exception rules are configured in a single AddResilience call.
    //  The On<Exception>() catch-all provides global retry/redelivery defaults.
    // -----------------------------------------------------------------------
    .AddResilience(policy =>
    {
        // --- Terminal: DeadLetter ---
        // Validation errors are permanent  the message payload is bad.
        // Skip retry and redelivery entirely; route straight to the error endpoint.
        policy.On<MessageValidationException>().DeadLetter();

        // --- Terminal: Discard ---
        // Duplicate messages are expected in at-least-once delivery systems.
        // Silently drop them  no retry, no redelivery, no error endpoint.
        policy.On<DuplicateMessageException>().Discard();

        // --- Retry only (skip redelivery) ---
        // Payment gateway is flaky but usually recovers within a few attempts.
        // Retry 5 times with exponential backoff, then dead-letter on exhaustion.
        policy.On<PaymentGatewayException>()
            .Retry(
                attempts: 5,
                delay: TimeSpan.FromMilliseconds(200),
                backoff: RetryBackoffType.Exponential);

        // --- Redeliver only (skip retry) ---
        // Auth token expired  immediate retry is pointless because the token
        // won't refresh in milliseconds. Wait for redelivery instead.
        policy.On<AuthTokenExpiredException>().Redeliver();

        // --- Escalation: Retry then Redeliver ---
        // Transient DB errors  try a few times quickly (connection hiccup),
        // then back off with redelivery if the database is truly struggling.
        policy.On<TransientDatabaseException>()
            .Retry(attempts: 3)
            .ThenRedeliver();

        // --- Escalation: Retry then DeadLetter (skip redelivery) ---
        // Poison messages  try once in case it was a transient parse glitch,
        // then give up immediately. Redelivery won't fix a corrupt payload.
        policy.On<PoisonMessageException>()
            .Retry(attempts: 1)
            .ThenDeadLetter();

        // --- Full chain: Retry -> Redeliver -> DeadLetter ---
        // External service completely down  aggressive retry first, then
        // patient redelivery with increasing intervals, then dead-letter
        // as the last resort so operators can investigate.
        policy.On<ExternalServiceUnavailableException>()
            .Retry(attempts: 5, delay: TimeSpan.FromMilliseconds(500))
            .ThenRedeliver(
            [
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60)
            ])
            .ThenDeadLetter();

        // --- Conditional: Different policies for the same exception type ---
        // HTTP 404 = resource is gone permanently, dead-letter it.
        policy.On<HttpServiceException>(ex => ex.StatusCode == 404)
            .DeadLetter();

        // HTTP 429 = rate limited, back off with redelivery.
        policy.On<HttpServiceException>(ex => ex.StatusCode == 429)
            .Redeliver(
            [
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30)
            ]);

        // HTTP 503 = service unavailable, retry quickly then redeliver.
        policy.On<HttpServiceException>(ex => ex.StatusCode == 503)
            .Retry(attempts: 3)
            .ThenRedeliver();

        // --- Catch-all: Default for unmatched exceptions ---
        // Most-specific-type-wins means this only fires for exceptions
        // not matched by any of the rules above.
        policy.On<Exception>()
            .Retry(attempts: 2)
            .ThenRedeliver();
    })

    // -----------------------------------------------------------------------
    //  Register handlers
    // -----------------------------------------------------------------------
    .AddEventHandler<ProcessPaymentHandler>()
    .AddEventHandler<ValidateOrderHandler>()
    .AddEventHandler<DeduplicateMessageHandler>()
    .AddEventHandler<CallExternalApiHandler>()
    .AddEventHandler<RefreshAuthTokenHandler>()
    .AddEventHandler<ProcessBatchHandler>()
    .AddEventHandler<IngestTelemetryHandler>()
    .AddEventHandler<HandlePoisonMessageHandler>()

    // -----------------------------------------------------------------------
    //  Transport  InMemory for this demo (no external dependencies)
    // -----------------------------------------------------------------------
    .AddInMemory();

var app = builder.Build();
await app.RunAsync();
