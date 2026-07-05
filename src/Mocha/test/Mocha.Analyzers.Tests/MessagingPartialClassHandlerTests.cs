using Microsoft.CodeAnalysis;

namespace Mocha.Analyzers.Tests;

public class MessagingPartialClassHandlerTests
{
    private static readonly string[] s_partialHandlerRestatesInterfaceSources =
    [
        """
        using System.Threading;
        using System.Threading.Tasks;
        using Mocha;

        namespace TestApp;

        public record OrderPlacedEvent(int OrderId);

        public partial class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
        {
            public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                => default;
        }
        """,
        """
        using Mocha;

        namespace TestApp;

        public partial class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>;
        """
    ];

    [Fact]
    public async Task Generate_Should_EmitSingleInitializer_When_PartialHandlerRestatesInterfaceOnBothParts()
    {
        // The generator must collapse the two per-part event handler infos into one consumer
        // registration and one consumer initializer method.
        await MessagingTestHelper.GetGeneratedSourceSnapshot(s_partialHandlerRestatesInterfaceSources)
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Generate_Should_Compile_When_PartialHandlerRestatesInterfaceOnBothParts()
    {
        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics(s_partialHandlerRestatesInterfaceSources);

        // assert
        AssertNoErrors(diagnostics);
    }

    private static void AssertNoErrors(IReadOnlyCollection<Diagnostic> diagnostics)
    {
        var errors = diagnostics
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Select(static d => d.ToString())));
    }

    [Fact]
    public async Task Generate_Should_EmitSingleConsumerInitializer_When_PartialEventHandlerRepeatsInterfaceOnBothParts()
    {
        // The generator must collapse the two per-part event handler infos into one consumer
        // registration and one consumer initializer method.
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public record OrderPlacedEvent(int OrderId);

            public partial class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }
            """,
            """
            using Mocha;

            namespace TestApp;

            public partial class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>;
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_EmitSingleSagaInitializer_When_PartialSagaRepeatsBaseTypeOnBothParts()
    {
        // The generator must collapse the two per-part saga infos into one saga registration and
        // one saga initializer method.
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Sagas;

            namespace TestApp;

            public class OrderState : SagaStateBase;

            public partial class OrderFulfillmentSaga : Saga<OrderState>
            {
                protected override void Configure(ISagaDescriptor<OrderState> descriptor)
                {
                }
            }
            """,
            """
            using Mocha.Sagas;

            namespace TestApp;

            public partial class OrderFulfillmentSaga : Saga<OrderState>;
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
