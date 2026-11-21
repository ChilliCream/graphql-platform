using Microsoft.CodeAnalysis;

namespace Mocha.Analyzers.Tests;

public class OutboxCallSiteTests
{
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
}
