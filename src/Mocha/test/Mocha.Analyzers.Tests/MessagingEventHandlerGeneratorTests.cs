namespace Mocha.Analyzers.Tests;

public class MessagingEventHandlerGeneratorTests
{
    [Fact]
    public async Task Generate_SingleEventHandler_MatchesSnapshot()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public record OrderPlacedEvent(int OrderId);

            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_MultipleEventHandlers_MatchesSnapshot()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public record OrderPlacedEvent(int OrderId);

            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }

            public class OrderPlacedAuditHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_EmitAddMessageAndSource_When_HandlerMessageDocumentedWithoutJsonContext()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            /// <summary>
            /// Published after an order is placed.
            /// </summary>
            public record OrderPlacedEvent(int OrderId);

            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_EmitTrackingMetadata_When_MessageAndHandlerHaveXmlDocumentation()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using System.Text.Json.Serialization;
            using Mocha;

            [assembly: MessagingModule("TestApp", JsonContext = typeof(TestApp.TestJsonContext))]

            namespace TestApp;

            /// <summary>
            /// Published after an order is placed.
            /// </summary>
            public record OrderPlacedEvent(int OrderId);

            /// <summary>
            /// Handles order placed events.
            /// </summary>
            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }

            [JsonSerializable(typeof(OrderPlacedEvent))]
            public partial class TestJsonContext : JsonSerializerContext;
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_EmitTrackingMetadata_When_MessageOnlyComesFromJsonContext()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using System.Text.Json.Serialization;
            using Mocha;

            [assembly: MessagingModule("TestApp", JsonContext = typeof(TestApp.TestJsonContext))]

            namespace TestApp;

            public record InventoryAdjustedEvent(int ProductId);

            [JsonSerializable(typeof(InventoryAdjustedEvent))]
            public partial class TestJsonContext : JsonSerializerContext;
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_OmitSourceMetadata_When_EmitSourceMetadataDisabled()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using System.Text.Json.Serialization;
            using Mocha;

            [assembly: MessagingModule("TestApp", JsonContext = typeof(TestApp.TestJsonContext))]

            namespace TestApp;

            /// <summary>
            /// Published after an order is placed.
            /// </summary>
            public record OrderPlacedEvent(int OrderId);

            /// <summary>
            /// Handles order placed events.
            /// </summary>
            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }

            [JsonSerializable(typeof(OrderPlacedEvent))]
            public partial class TestJsonContext : JsonSerializerContext;
            """
        ],
        emitSourceMetadata: false).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_EmitFileNameOnly_When_NoSourceRoots()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using System.Text.Json.Serialization;
            using Mocha;

            [assembly: MessagingModule("TestApp", JsonContext = typeof(TestApp.TestJsonContext))]

            namespace TestApp;

            /// <summary>
            /// Published after an order is placed.
            /// </summary>
            public record OrderPlacedEvent(int OrderId);

            /// <summary>
            /// Handles order placed events.
            /// </summary>
            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }

            [JsonSerializable(typeof(OrderPlacedEvent))]
            public partial class TestJsonContext : JsonSerializerContext;
            """
        ],
        sourcePaths: ["C:\\app\\Handlers\\OrderHandlers.cs"]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_EmitDirectory_When_SourceRootMatches()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using System.Text.Json.Serialization;
            using Mocha;

            [assembly: MessagingModule("TestApp", JsonContext = typeof(TestApp.TestJsonContext))]

            namespace TestApp;

            /// <summary>
            /// Published after an order is placed.
            /// </summary>
            public record OrderPlacedEvent(int OrderId);

            /// <summary>
            /// Handles order placed events.
            /// </summary>
            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }

            [JsonSerializable(typeof(OrderPlacedEvent))]
            public partial class TestJsonContext : JsonSerializerContext;
            """
        ],
        sourcePaths: ["/repo/src/Order/Handlers/OrderHandlers.cs"],
        sourceRoots: "/repo/>>git").MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
