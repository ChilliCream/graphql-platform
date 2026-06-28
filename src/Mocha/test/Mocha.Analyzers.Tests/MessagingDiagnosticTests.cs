using Microsoft.CodeAnalysis;

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
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
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
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
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
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
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
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void MO0015_Should_Report_When_PublishAotAndCallSiteRequiresJsonContext()
    {
        // arrange
        var diagnostics = MessagingTestHelper.GetGeneratorDiagnostics(
        [
            """
            using System.Threading;
            using System.Threading.Tasks;
            using Mocha;

            namespace TestApp;

            public record OrderPlacedEvent(int OrderId);

            public sealed class Dispatcher
            {
                public ValueTask DispatchAsync(IMessageBus bus, CancellationToken cancellationToken)
                    => bus.PublishAsync(new OrderPlacedEvent(1), cancellationToken);
            }
            """
        ],
        publishAot: true);

        // assert
        var diagnostic = Assert.Single(diagnostics, d => d.Id == "MO0015");
        Assert.Contains("Tests", diagnostic.GetMessage());
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

    [Fact]
    public void Generate_Should_Compile_When_SagaOnlyModuleHasNoJsonContext()
    {
        // arrange
        const string source =
            """
            using Mocha;
            using Mocha.Sagas;

            [assembly: MessagingModule("TestApp")]

            namespace TestApp;

            public class OrderState : SagaStateBase;

            public class OrderSaga : Saga<OrderState>
            {
                protected override void Configure(ISagaDescriptor<OrderState> descriptor)
                {
                }
            }
            """;

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);

        // assert
        AssertNoErrors(diagnostics);
    }

    [Fact]
    public void Generate_Should_Compile_When_JsonContextRegistersTypeInfoResolver()
    {
        // arrange
        const string source =
            """
            using System;
            using System.Text.Json;
            using System.Text.Json.Serialization;
            using System.Text.Json.Serialization.Metadata;
            using System.Threading;
            using System.Threading.Tasks;
            using Mocha;

            [assembly: MessagingModule("TestApp", JsonContext = typeof(TestApp.TestJsonContext))]

            namespace TestApp;

            [JsonSerializable(typeof(OrderPlacedEvent))]
            public sealed class TestJsonContext : IJsonTypeInfoResolver
            {
                public static TestJsonContext Default { get; } = new();

                public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
                    => null;

                public JsonTypeInfo? GetTypeInfo(Type type)
                    => null;
            }

            public record OrderPlacedEvent(int OrderId);

            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }
            """;

        // act
        var diagnostics = MessagingTestHelper.GetCompilationDiagnostics([source]);

        // assert
        AssertNoErrors(diagnostics);
    }

    [Fact]
    public void MO0018_Should_Report_When_MessageBusCallSiteTypesAreMissingFromJsonContext()
    {
        // arrange
        var diagnostics = MessagingTestHelper.GetGeneratorDiagnostics(
        [
            """
            using System.Text.Json.Serialization;
            using System.Threading;
            using System.Threading.Tasks;
            using Mocha;

            [assembly: MessagingModule("TestApp", JsonContext = typeof(TestApp.TestJsonContext))]

            namespace TestApp;

            [JsonSerializable(typeof(RequestMessage))]
            public sealed class TestJsonContext;

            public sealed record PublishMessage;

            public sealed record SendMessage;

            public sealed record RequestMessage : IEventRequest<ResponseMessage>;

            public sealed record ResponseMessage;

            public sealed class Dispatcher
            {
                public async ValueTask DispatchAsync(IMessageBus bus, CancellationToken cancellationToken)
                {
                    await bus.PublishAsync(new PublishMessage(), cancellationToken);
                    await bus.SendAsync(new SendMessage(), cancellationToken);
                    await bus.RequestAsync(new RequestMessage(), cancellationToken);
                }
            }
            """
        ],
        publishAot: true);

        // assert
        var messages = diagnostics
            .Where(d => d.Id == "MO0018")
            .Select(static d => d.GetMessage())
            .OrderBy(static m => m)
            .ToArray();

        Assert.Collection(
            messages,
            static m =>
            {
                Assert.Contains("TestApp.PublishMessage", m);
                Assert.Contains("Publish", m);
            },
            static m =>
            {
                Assert.Contains("TestApp.ResponseMessage", m);
                Assert.Contains("Request", m);
            },
            static m =>
            {
                Assert.Contains("TestApp.SendMessage", m);
                Assert.Contains("Send", m);
            });
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

    private static void AssertNoErrors(IReadOnlyCollection<Microsoft.CodeAnalysis.Diagnostic> diagnostics)
    {
        var errors = diagnostics
            .Where(static d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToArray();

        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Select(static d => d.ToString())));
    }
}
