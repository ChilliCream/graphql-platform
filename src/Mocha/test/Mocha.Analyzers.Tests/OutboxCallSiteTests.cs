using CookieCrumble;
using Microsoft.CodeAnalysis;

namespace Mocha.Analyzers.Tests;

public class OutboxCallSiteTests
{
    [Theory]
    [InlineData("db.Outbox(bus).PublishAsync(new OutboundMessage(), cancellationToken)")]
    [InlineData("db.Outbox(bus).PublishAsync<OutboundMessage>(new(), cancellationToken)")]
    [InlineData("db.Outbox(bus).PublishAsync(new OutboundMessage(), PublishOptions.Default, cancellationToken)")]
    [InlineData("outbox.PublishAsync(new OutboundMessage(), cancellationToken)")]
    [InlineData("outbox.PublishAsync<OutboundMessage>(new(), PublishOptions.Default, cancellationToken)")]
    [InlineData("typedOutbox.PublishAsync(new OutboundMessage(), cancellationToken)")]
    [InlineData("typedOutbox.PublishAsync<OutboundMessage>(new(), PublishOptions.Default, cancellationToken)")]
    [InlineData("db.Outbox(bus).SendAsync(new OutboundMessage(), cancellationToken)")]
    [InlineData("db.Outbox(bus).SendAsync<OutboundMessage>(new(), cancellationToken)")]
    [InlineData("db.Outbox(bus).SendAsync(new OutboundMessage(), SendOptions.Default, cancellationToken)")]
    [InlineData("outbox.SendAsync(new OutboundMessage(), cancellationToken)")]
    [InlineData("outbox.SendAsync<OutboundMessage>(new(), SendOptions.Default, cancellationToken)")]
    [InlineData("typedOutbox.SendAsync(new OutboundMessage(), cancellationToken)")]
    [InlineData("typedOutbox.SendAsync<OutboundMessage>(new(), SendOptions.Default, cancellationToken)")]
    [InlineData("db.Outbox(bus).PublishAsync(new OutboundMessage(), new PublishOptions { ScheduledTime = System.DateTimeOffset.UtcNow }, cancellationToken)")]
    [InlineData("db.Outbox(bus).SendAsync(new OutboundMessage(), new SendOptions { ScheduledTime = System.DateTimeOffset.UtcNow }, cancellationToken)")]
    [InlineData("outbox.PublishAsync<OutboundMessage>(new(), new PublishOptions { ScheduledTime = System.DateTimeOffset.UtcNow }, cancellationToken)")]
    [InlineData("outbox.SendAsync<OutboundMessage>(new(), new SendOptions { ScheduledTime = System.DateTimeOffset.UtcNow }, cancellationToken)")]
    [InlineData("typedOutbox.PublishAsync(new OutboundMessage(), new PublishOptions { ScheduledTime = System.DateTimeOffset.UtcNow }, cancellationToken)")]
    [InlineData("typedOutbox.SendAsync(new OutboundMessage(), new SendOptions { ScheduledTime = System.DateTimeOffset.UtcNow }, cancellationToken)")]
    public void Generate_Should_RegisterOutboundMessage_When_OutboxDispatches(string expression)
    {
        // arrange
        var source = CreateSource(expression);

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var registrations = GetMessageRegistrations(source);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        registrations.MatchInlineSnapshot(
            """
            [
              "global::Mocha.MessageBusHostBuilderExtensions.AddMessage<global::TestApp.IncomingMessage>(builder);",
              "global::Mocha.MessageBusHostBuilderExtensions.AddMessage<global::TestApp.OutboundMessage>(builder);"
            ]
            """);
    }

    [Theory]
    [InlineData("db.Outbox(bus)", "PublishAsync", "Publish", "PublishOptions")]
    [InlineData("db.Outbox(bus)", "SendAsync", "Send", "SendOptions")]
    [InlineData("outbox", "PublishAsync", "Publish", "PublishOptions")]
    [InlineData("outbox", "SendAsync", "Send", "SendOptions")]
    [InlineData("typedOutbox", "PublishAsync", "Publish", "PublishOptions")]
    [InlineData("typedOutbox", "SendAsync", "Send", "SendOptions")]
    public void Generate_Should_ReportMissingJsonType_When_OutboxDispatchesInAotMode(
        string receiver,
        string method,
        string kind,
        string options)
    {
        // arrange
        var source = CreateSource(
            $"{receiver}.{method}(new OutboundMessage(), {options}.Default, cancellationToken)",
            jsonContext: true);

        // act
        var diagnostics = MessagingTestHelper.GetGeneratorDiagnostics([source], publishAot: true);

        // assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MO0018", diagnostic.Id);
        Assert.Equal(
            $"Type 'global::TestApp.OutboundMessage' is used in a {kind} call but is not included "
            + "in JsonSerializerContext 'global::TestApp.TestJsonContext'. "
            + "Add [JsonSerializable(typeof(global::TestApp.OutboundMessage))] to the context.",
            diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("db.Outbox(bus)", "PublishAsync")]
    [InlineData("db.Outbox(bus)", "SendAsync")]
    [InlineData("outbox", "PublishAsync")]
    [InlineData("outbox", "SendAsync")]
    [InlineData("typedOutbox", "PublishAsync")]
    [InlineData("typedOutbox", "SendAsync")]
    public void Generate_Should_AcceptJsonType_When_OutboxMessageIsRegistered(string receiver, string method)
    {
        // arrange
        var source = CreateSource(
            $"{receiver}.{method}(new OutboundMessage(), cancellationToken)",
            jsonContext: true,
            registerOutboundMessage: true);

        // act
        var diagnostics = MessagingTestHelper.GetGeneratorDiagnostics([source], publishAot: true);

        // assert
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("PublishAsync")]
    [InlineData("SendAsync")]
    public void Generate_Should_IgnoreMessage_When_UnrelatedOutboxDispatches(string method)
    {
        // arrange
        var source = CreateSource($"unrelated.{method}(new OutboundMessage(), cancellationToken)");

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var registrations = GetMessageRegistrations(source);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        registrations.MatchInlineSnapshot(
            """
            [
              "global::Mocha.MessageBusHostBuilderExtensions.AddMessage<global::TestApp.IncomingMessage>(builder);"
            ]
            """);
    }

    [Theory]
    [InlineData("PublishAsync")]
    [InlineData("SendAsync")]
    public void Generate_Should_RegisterOutboundMessage_When_ApplicationHasNoHandlers(string method)
    {
        // arrange
        var source = $$"""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.EntityFrameworkCore;
            using Mocha;
            using Mocha.EntityFrameworkCore;

            [assembly: MessagingModule("TestApp")]

            namespace TestApp;

            public sealed record OutboundMessage;

            public sealed class Dispatcher
            {
                public ValueTask DispatchAsync(DbContext db, IMessageBus bus, CancellationToken cancellationToken)
                    => db.Outbox(bus).{{method}}(new OutboundMessage(), cancellationToken);
            }
            """;

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var registrations = GetMessageRegistrations(source);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        registrations.MatchInlineSnapshot(
            """
            [
              "global::Mocha.MessageBusHostBuilderExtensions.AddMessage<global::TestApp.OutboundMessage>(builder);"
            ]
            """);
    }

    [Fact]
    public void Generate_Should_IgnoreMediatorMessage_When_ApplicationHasNoBusDispatch()
    {
        // arrange
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Mocha.Mediator;

            namespace TestApp;

            public sealed record OutboundMessage : INotification;

            public sealed class Dispatcher
            {
                public ValueTask DispatchAsync(IPublisher publisher, CancellationToken cancellationToken)
                    => publisher.PublishAsync(new OutboundMessage(), cancellationToken);
            }
            """;

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var sources = MessagingTestHelper.GetGeneratedSourceTexts([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(sources);
    }

    private static string[] GetMessageRegistrations(string source)
        => MessagingTestHelper.GetGeneratedSourceTexts([source])
            .SelectMany(static s => s.Split('\n'))
            .Select(static line => line.Trim())
            .Where(static line => line.StartsWith(
                "global::Mocha.MessageBusHostBuilderExtensions.AddMessage<",
                StringComparison.Ordinal))
            .ToArray();

    private static string CreateSource(
        string expression,
        bool jsonContext = false,
        bool registerOutboundMessage = false)
    {
        var module = jsonContext
            ? "[assembly: MessagingModule(\"TestApp\", JsonContext = typeof(TestApp.TestJsonContext))]"
            : "[assembly: MessagingModule(\"TestApp\")]";
        var outboundRegistration = registerOutboundMessage
            ? "[JsonSerializable(typeof(OutboundMessage))]"
            : "";
        var context = jsonContext
            ? $$"""
              [JsonSerializable(typeof(IncomingMessage))]
              {{outboundRegistration}}
              public sealed class TestJsonContext;
              """
            : "";

        return $$"""
            using System.Text.Json.Serialization;
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.EntityFrameworkCore;
            using Mocha;
            using Mocha.EntityFrameworkCore;

            {{module}}

            namespace TestApp;

            {{context}}

            public sealed record IncomingMessage;

            public sealed record OutboundMessage;

            public sealed class IncomingHandler : IEventHandler<IncomingMessage>
            {
                public ValueTask HandleAsync(IncomingMessage message, CancellationToken cancellationToken)
                    => default;
            }

            public sealed class Dispatcher
            {
                public async ValueTask DispatchAsync(
                    DbContext db,
                    IMessageBus bus,
                    DbContextOutbox unrelated,
                    CancellationToken cancellationToken)
                {
                    var outbox = db.Outbox(bus);
                    Mocha.EntityFrameworkCore.DbContextOutbox typedOutbox = db.Outbox(bus);
                    await {{expression}};
                }
            }

            public sealed class DbContextOutbox
            {
                public ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken)
                    => default;

                public ValueTask SendAsync<T>(T message, CancellationToken cancellationToken)
                    => default;
            }
            """;
    }
}
