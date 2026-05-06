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

    [Fact]
    public void MO0016_Should_NotReport_When_JsonContextIsPartialAndPublishAotIsFalse()
    {
        // arrange
        var diagnostics = MessagingTestHelper.GetGeneratorDiagnostics(
        [
            """
            using System.Text.Json.Serialization;
            using Mocha;

            [assembly: MessagingModule("TestApp", JsonContext = typeof(TestApp.TestJsonContext))]

            namespace TestApp;

            [JsonSerializable(typeof(OrderPlacedEvent))]
            public partial class TestJsonContext : JsonSerializerContext;

            public record OrderPlacedEvent(int OrderId);

            public record OrderAcceptedEvent(int OrderId);

            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }

            public class OrderAcceptedHandler : IEventHandler<OrderAcceptedEvent>
            {
                public ValueTask HandleAsync(OrderAcceptedEvent message, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]);

        // assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MO0016");
    }

    [Fact]
    public void MO0016_Should_Report_When_JsonContextIsPartialAndPublishAotIsTrue()
    {
        // arrange
        var diagnostics = MessagingTestHelper.GetGeneratorDiagnostics(
        [
            """
            using System.Text.Json.Serialization;
            using Mocha;

            [assembly: MessagingModule("TestApp", JsonContext = typeof(TestApp.TestJsonContext))]

            namespace TestApp;

            [JsonSerializable(typeof(OrderPlacedEvent))]
            public partial class TestJsonContext : JsonSerializerContext;

            public record OrderPlacedEvent(int OrderId);

            public record OrderAcceptedEvent(int OrderId);

            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }

            public class OrderAcceptedHandler : IEventHandler<OrderAcceptedEvent>
            {
                public ValueTask HandleAsync(OrderAcceptedEvent message, CancellationToken cancellationToken)
                    => default;
            }
            """
        ],
        publishAot: true);

        // assert
        var diagnostic = Assert.Single(diagnostics, d => d.Id == "MO0016");
        Assert.Contains("OrderAcceptedEvent", diagnostic.GetMessage());
    }

    [Fact]
    public void Generate_Should_NotEnableAotStrictMode_When_JsonContextSpecifiedAndPublishAotIsFalse()
    {
        // arrange
        var source = CreateJsonContextSource();

        // act
        var generated = string.Join(
            "\n",
            MessagingTestHelper.GetGeneratedSourceTexts([source]));

        // assert
        Assert.Contains("AddJsonTypeInfoResolver", generated);
        Assert.DoesNotContain("IsAotCompatible = true", generated);
    }

    [Fact]
    public void Generate_Should_EnableAotStrictMode_When_JsonContextSpecifiedAndPublishAotIsTrue()
    {
        // arrange
        var source = CreateJsonContextSource();

        // act
        var generated = string.Join(
            "\n",
            MessagingTestHelper.GetGeneratedSourceTexts([source], publishAot: true));

        // assert
        Assert.Contains("AddJsonTypeInfoResolver", generated);
        Assert.Contains("IsAotCompatible = true", generated);
    }

    private static string CreateJsonContextSource()
        => """
        using System.Text.Json.Serialization;
        using Mocha;

        [assembly: MessagingModule("TestApp", JsonContext = typeof(TestApp.TestJsonContext))]

        namespace TestApp;

        [JsonSerializable(typeof(OrderPlacedEvent))]
        public partial class TestJsonContext : JsonSerializerContext;

        public record OrderPlacedEvent(int OrderId);

        public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
        {
            public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                => default;
        }
        """;
}
