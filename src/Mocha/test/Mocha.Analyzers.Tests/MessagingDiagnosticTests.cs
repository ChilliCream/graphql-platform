namespace Mocha.Analyzers.Tests;

public class MessagingDiagnosticTests
{
    [Fact]
    public async Task MO0011_DuplicateRequestHandler_ReportsError()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public record GetOrderRequest(int OrderId) : IEventRequest<string>;

            public class GetOrderHandlerA : IEventRequestHandler<GetOrderRequest, string>
            {
                public ValueTask<string> HandleAsync(GetOrderRequest request, CancellationToken cancellationToken)
                    => new("a");
            }

            public class GetOrderHandlerB : IEventRequestHandler<GetOrderRequest, string>
            {
                public ValueTask<string> HandleAsync(GetOrderRequest request, CancellationToken cancellationToken)
                    => new("b");
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MO0013_AbstractMessagingHandler_ReportsWarning()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public record OrderPlacedEvent(int OrderId);

            public abstract class BaseOrderHandler : IEventHandler<OrderPlacedEvent>
            {
                public abstract ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken);
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MO0012_OpenGenericHandler_ReportsInfo()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public class GenericHandler<T> : IEventHandler<T>
            {
                public ValueTask HandleAsync(T message, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MO0014_SagaWithoutParameterlessConstructor_ReportsError()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Sagas;

            namespace TestApp;

            public class OrderState : SagaStateBase;

            public class BadSaga : Saga<OrderState>
            {
                public BadSaga(string name) { }

                protected override void Configure(ISagaDescriptor<OrderState> descriptor) { }
            }
            """
        ]).MatchMarkdownAsync();
    }
}
