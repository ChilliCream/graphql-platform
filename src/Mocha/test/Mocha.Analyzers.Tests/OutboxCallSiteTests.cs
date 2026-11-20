using CookieCrumble;
using Microsoft.CodeAnalysis;

namespace Mocha.Analyzers.Tests;

public class OutboxCallSiteTests
{
    [Theory]
    [InlineData("db.Outbox(bus).PublishAsync(new OutboundMessage(), cancellationToken)")]
    [InlineData("db.Outbox(bus).SendAsync(new OutboundMessage(), cancellationToken)")]
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

    [Fact]
    public void Generate_Should_ReportMissingJsonType_When_OutboxDispatchesInAotMode()
    {
        // arrange
        var source = CreateSource(
            "db.Outbox(bus).PublishAsync(new OutboundMessage(), PublishOptions.Default, cancellationToken)",
            jsonContext: true);

        // act
        var diagnostics = MessagingTestHelper.GetGeneratorDiagnostics([source], publishAot: true);

        // assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MO0018", diagnostic.Id);
        Assert.Equal(
            "Type 'global::TestApp.OutboundMessage' is used in a Publish call but is not included "
            + "in JsonSerializerContext 'global::TestApp.TestJsonContext'. "
            + "Add [JsonSerializable(typeof(global::TestApp.OutboundMessage))] to the context.",
            diagnostic.GetMessage());
    }

    [Fact]
    public void Generate_Should_AcceptJsonType_When_OutboxMessageIsRegistered()
    {
        // arrange
        var source = CreateSource(
            "db.Outbox(bus).PublishAsync(new OutboundMessage(), cancellationToken)",
            jsonContext: true,
            registerOutboundMessage: true);

        // act
        var diagnostics = MessagingTestHelper.GetGeneratorDiagnostics([source], publishAot: true);

        // assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Generate_Should_IgnoreMessage_When_UnrelatedOutboxDispatches()
    {
        // arrange
        var source = CreateSource("unrelated.PublishAsync(new OutboundMessage(), cancellationToken)");

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

    [Fact]
    public void Generate_Should_RegisterOutboundMessage_When_ApplicationHasNoHandlers()
    {
        // arrange
        const string source = """
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
                    => db.Outbox(bus).PublishAsync(new OutboundMessage(), cancellationToken);
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

    [Theory]
    [InlineData("await bus.PublishAsync(new Ping(), cancellationToken);")]
    [InlineData("await db.Outbox(bus).PublishAsync(new Ping(), cancellationToken);")]
    public void Generate_Should_SkipMessage_When_PublishedTypeIsPrivateNested(string dispatch)
    {
        // arrange
        var source = CreatePublisherSource(dispatch, nestedTypes: "private sealed record Ping;");

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var sources = MessagingTestHelper.GetGeneratedSourceTexts([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(sources);
    }

    [Theory]
    [InlineData("protected sealed record Ping;", "new Ping()")]
    [InlineData("private protected sealed record Ping;", "new Ping()")]
    [InlineData("protected class Messages { public sealed record Ping; }", "new Messages.Ping()")]
    public void Generate_Should_SkipMessage_When_PublishedTypeIsProtectedNested(string nestedTypes, string message)
    {
        // arrange
        var source = CreatePublisherSource(
            $"await bus.PublishAsync({message}, cancellationToken);",
            nestedTypes: nestedTypes);

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var sources = MessagingTestHelper.GetGeneratedSourceTexts([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(sources);
    }

    [Fact]
    public void Generate_Should_SkipMessage_When_PublishedTypeIsFileLocal()
    {
        // arrange
        var source = CreatePublisherSource(
            "await bus.PublishAsync(new Ping(), cancellationToken);",
            topLevelTypes: "file sealed record Ping;");

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var sources = MessagingTestHelper.GetGeneratedSourceTexts([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(sources);
    }

    [Theory]
    [InlineData("new Envelope<Ping>(new Ping())")]
    [InlineData("new Envelope<(Ping, int)>((new Ping(), 1))")]
    public void Generate_Should_SkipMessage_When_TypeArgumentIsPrivateNested(string message)
    {
        // arrange
        var source = CreatePublisherSource(
            $"await bus.PublishAsync({message}, cancellationToken);",
            nestedTypes: "private sealed record Ping;",
            topLevelTypes: "public sealed record Envelope<T>(T Payload);");

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var sources = MessagingTestHelper.GetGeneratedSourceTexts([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(sources);
    }

    [Theory]
    [InlineData("bus.PublishAsync(new Envelope<T>(payload), cancellationToken)")]
    [InlineData("db.Outbox(bus).PublishAsync(new Envelope<T>(payload), cancellationToken)")]
    public void Generate_Should_SkipMessage_When_TypeArgumentIsTypeParameter(string dispatch)
    {
        // arrange
        var source = CreateEnvelopeSource(
            $$"""
            public ValueTask PublishAsync<T>(
                DbContext db,
                IMessageBus bus,
                T payload,
                CancellationToken cancellationToken)
                => {{dispatch}};
            """);

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var sources = MessagingTestHelper.GetGeneratedSourceTexts([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(sources);
    }

    [Fact]
    public void Generate_Should_SkipMessage_When_ContainingTypeHasTypeParameter()
    {
        // arrange
        var source = CreateEnvelopeSource(
            """
            public sealed class Feature<T>
            {
                public ValueTask PublishAsync(IMessageBus bus, CancellationToken cancellationToken)
                    => bus.PublishAsync(new Started(), cancellationToken);

                public sealed record Started;
            }
            """);

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var sources = MessagingTestHelper.GetGeneratedSourceTexts([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(sources);
    }

    [Fact]
    public void Generate_Should_SkipMessage_When_TypeArgumentIsAnonymousType()
    {
        // arrange
        var source = CreateEnvelopeSource(
            """
            public ValueTask PublishAsync(IMessageBus bus, CancellationToken cancellationToken)
                => bus.PublishAsync(Envelope.Create(new { OrderId = 1 }), cancellationToken);
            """);

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var sources = MessagingTestHelper.GetGeneratedSourceTexts([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(sources);
    }

    [Theory]
    [InlineData("int*[]")]
    [InlineData("delegate*<void>[]")]
    public void Generate_Should_SkipMessage_When_TypeArgumentContainsPointerType(string typeArgument)
    {
        // arrange
        var source = CreateEnvelopeSource(
            $$"""
            public unsafe ValueTask PublishAsync(IMessageBus bus, CancellationToken cancellationToken)
                => bus.PublishAsync(new Envelope<{{typeArgument}}>([]), cancellationToken);
            """);

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var sources = MessagingTestHelper.GetGeneratedSourceTexts([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(sources);
    }

    [Fact]
    public void Generate_Should_SkipMessage_When_TypeArgumentDoesNotExist()
    {
        // arrange
        var source = CreateEnvelopeSource(
            """
            public ValueTask PublishAsync(IMessageBus bus, CancellationToken cancellationToken)
                => bus.PublishAsync(new Envelope<Missing>(null!), cancellationToken);
            """);

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var sources = MessagingTestHelper.GetGeneratedSourceTexts([source]);

        // assert
        Assert.Equal(
            [
                "(21,38): error CS0246: The type or namespace name 'Missing' could not be found "
                + "(are you missing a using directive or an assembly reference?)"
            ],
            diagnostics
                .Where(static d => d.Severity == DiagnosticSeverity.Error)
                .Select(static d => d.ToString()));
        Assert.Empty(sources);
    }

    [Fact]
    public async Task Generate_Should_RegisterMessage_When_TypeArgumentIsAccessible()
    {
        // arrange
        var source = CreateEnvelopeSource(
            """
            public async ValueTask PublishAsync(IMessageBus bus, CancellationToken cancellationToken)
            {
                await bus.PublishAsync(new Envelope<OrderPlaced>(new OrderPlaced()), cancellationToken);
                await bus.PublishAsync(new Envelope<OrderPlaced[]>([]), cancellationToken);
            }
            """);

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var snapshot = MessagingTestHelper.GetGeneratedSourceSnapshot([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_RegisterMessage_When_PublishedTypeIsInternalNested()
    {
        // arrange
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Mocha;

            namespace TestApp;

            internal sealed class Messages
            {
                internal sealed record OrderPlaced;
            }

            public class Dispatcher
            {
                public async ValueTask DispatchAsync(IMessageBus bus, CancellationToken cancellationToken)
                {
                    await bus.PublishAsync(new Messages.OrderPlaced(), cancellationToken);
                    await bus.PublishAsync(new OrderShipped(), cancellationToken);
                }

                protected internal sealed record OrderShipped;
            }
            """;

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var snapshot = MessagingTestHelper.GetGeneratedSourceSnapshot([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_SkipPrivateNestedMessage_When_AssemblyHasHandler()
    {
        // arrange
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Mocha;

            namespace TestApp;

            public sealed record IncomingMessage;

            public sealed class IncomingHandler : IEventHandler<IncomingMessage>
            {
                public ValueTask HandleAsync(IncomingMessage message, CancellationToken cancellationToken)
                    => default;
            }

            public sealed class Dispatcher
            {
                public ValueTask DispatchAsync(IMessageBus bus, CancellationToken cancellationToken)
                    => bus.PublishAsync(new Ping(), cancellationToken);

                private sealed record Ping;
            }
            """;

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var snapshot = MessagingTestHelper.GetGeneratedSourceSnapshot([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_RegisterOnlyPublicMessage_When_PublicAndPrivateNestedTypesArePublished()
    {
        // arrange
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Mocha;

            namespace TestApp;

            public sealed record OrderPlaced;

            public sealed class Dispatcher
            {
                public async ValueTask DispatchAsync(IMessageBus bus, CancellationToken cancellationToken)
                {
                    await bus.PublishAsync(new OrderPlaced(), cancellationToken);
                    await bus.PublishAsync(new Ping(), cancellationToken);
                }

                private sealed record Ping;
            }
            """;

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var snapshot = MessagingTestHelper.GetGeneratedSourceSnapshot([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
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

    private static string CreatePublisherSource(
        string dispatch,
        string nestedTypes = "",
        string topLevelTypes = "")
        => $$"""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.EntityFrameworkCore;
            using Mocha;
            using Mocha.EntityFrameworkCore;

            namespace TestApp;

            {{topLevelTypes}}

            public class Dispatcher
            {
                public async ValueTask DispatchAsync(DbContext db, IMessageBus bus, CancellationToken cancellationToken)
                {
                    {{dispatch}}
                }

                {{nestedTypes}}
            }
            """;

    private static string CreateEnvelopeSource(string dispatcherMembers)
        => $$"""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.EntityFrameworkCore;
            using Mocha;
            using Mocha.EntityFrameworkCore;

            namespace TestApp;

            public sealed record OrderPlaced;

            public sealed record Envelope<T>(T Payload);

            public static class Envelope
            {
                public static Envelope<T> Create<T>(T payload) => new(payload);
            }

            public sealed class Dispatcher
            {
                {{dispatcherMembers}}
            }
            """;

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
