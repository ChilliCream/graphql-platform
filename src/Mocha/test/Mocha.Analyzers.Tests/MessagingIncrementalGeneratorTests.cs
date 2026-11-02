namespace Mocha.Analyzers.Tests;

public class MessagingIncrementalGeneratorTests
{
    [Fact]
    public async Task Generate_Should_RefreshMessageMetadata_When_OnlyMessageFileChanges()
    {
        // arrange
        // The handler and JsonContext live in a file that never changes; only the message file is edited.
        const string handlerFile =
            """
            using System.Text.Json.Serialization;
            using Mocha;

            [assembly: MessagingModule("TestApp", JsonContext = typeof(TestApp.TestJsonContext))]

            namespace TestApp;

            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken) => default;
            }

            [JsonSerializable(typeof(OrderPlacedEvent))]
            public partial class TestJsonContext : JsonSerializerContext;
            """;

        const string messageFile =
            """
            namespace TestApp;

            /// <summary>
            /// Old doc.
            /// </summary>
            public record OrderPlacedEvent(int OrderId);
            """;

        // The doc text changes and two extra blank lines shift both the start and end line of the record.
        const string updatedMessageFile =
            """
            namespace TestApp;



            /// <summary>
            /// New doc.
            /// </summary>
            public record OrderPlacedEvent(int OrderId);
            """;

        // act + assert
        await MessagingTestHelper
            .GetIncrementalGeneratedSourceSnapshot(
                [handlerFile, messageFile],
                replaceIndex: 1,
                updatedMessageFile)
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
