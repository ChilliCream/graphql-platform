using System.Collections.Concurrent;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

public sealed record TaggedOrder(string OrderId);

[Collection(JetStreamCollection.Name)]
public class CustomHeaderTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task PublishAsync_Should_PropagateHeaders_When_CustomHeadersSet()
    {
        // arrange
        // Includes a repeated header, which NATS carries as several values under one key and which the
        // writer previously collapsed to the array's type name.
        var cancellationToken = TestContext.Current.CancellationToken;
        var capture = new HeaderCapture();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(capture);
        builder.Services
            .AddMessageBus()
            .AddConsumer<HeaderSpyConsumer>()
            .AddNats(nats => nats.StreamName("header-service"));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(
                    new TaggedOrder("ORD-HDR"),
                    new PublishOptions
                    {
                        Headers = new()
                        {
                            ["x-tenant"] = "acme",
                            ["x-trace-id"] = "trace-123",
                            ["x-forwarded-for"] = new[] { "10.0.0.1", "10.0.0.2" }
                        }
                    },
                    cancellationToken);

            // assert
            Assert.True(await capture.WaitAsync(s_timeout), "The consumer never received the message.");

            Snapshot.Create()
                .Add(Describe(Assert.Single(capture.CapturedHeaders)))
                .MatchMarkdown();
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Renders the received headers, excluding the transport-owned keys.
    /// </summary>
    private static string Describe(Dictionary<string, object?> headers)
        => string.Join(
            "\n",
            headers
                .Where(h => !h.Key.StartsWith("x-", StringComparison.Ordinal)
                    || h.Key is "x-tenant" or "x-trace-id" or "x-forwarded-for")
                .OrderBy(h => h.Key, StringComparer.Ordinal)
                .Select(h => $"{h.Key} = {Render(h.Value)}"));

    private static string Render(object? value) => value switch
    {
        null => "(null)",
        string[] values => string.Join(" | ", values),
        _ => value.ToString() ?? "(null)"
    };

    public sealed class HeaderCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);

        public ConcurrentBag<Dictionary<string, object?>> CapturedHeaders { get; } = [];

        public void Record(IConsumeContext<TaggedOrder> context)
        {
            var headers = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var header in context.Headers)
            {
                headers[header.Key] = header.Value;
            }

            CapturedHeaders.Add(headers);
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout)
            => await _semaphore.WaitAsync(timeout);
    }

    public sealed class HeaderSpyConsumer(HeaderCapture capture) : IConsumer<TaggedOrder>
    {
        public ValueTask ConsumeAsync(IConsumeContext<TaggedOrder> context)
        {
            capture.Record(context);
            return default;
        }
    }
}
