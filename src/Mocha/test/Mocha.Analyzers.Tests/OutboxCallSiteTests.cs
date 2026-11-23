using Microsoft.CodeAnalysis;

namespace Mocha.Analyzers.Tests;

public class OutboxCallSiteTests
{
    [Theory]
    [InlineData("await bus.PublishAsync(new Ping(), cancellationToken);")]
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
    public void Generate_Should_SkipMessage_When_TypeArgumentIsTypeParameter(string dispatch)
    {
        // arrange
        var source = CreateEnvelopeSource(
            $$"""
            public ValueTask PublishAsync<T>(
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
    [InlineData("IDomainEvent message = new Ping(); await bus.PublishAsync(message, cancellationToken);")]
    [InlineData("EventBase message = new Pong(); await bus.SendAsync(message, cancellationToken);")]
    public void Generate_Should_SkipMessage_When_PublishedTypeIsInterfaceOrAbstract(string dispatch)
    {
        // arrange
        var source = CreatePublisherSource(
            dispatch,
            topLevelTypes: """
                public interface IDomainEvent;

                public sealed record Ping : IDomainEvent;

                public abstract record EventBase;

                public sealed record Pong : EventBase;
                """);

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);
        var sources = MessagingTestHelper.GetGeneratedSourceTexts([source]);

        // assert
        Assert.Empty(diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
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

    private static string CreatePublisherSource(
        string dispatch,
        string nestedTypes = "",
        string topLevelTypes = "")
        => $$"""
            using System.Threading;
            using System.Threading.Tasks;
            using Mocha;

            namespace TestApp;

            {{topLevelTypes}}

            public class Dispatcher
            {
                public async ValueTask DispatchAsync(IMessageBus bus, CancellationToken cancellationToken)
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
            using Mocha;

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
}
