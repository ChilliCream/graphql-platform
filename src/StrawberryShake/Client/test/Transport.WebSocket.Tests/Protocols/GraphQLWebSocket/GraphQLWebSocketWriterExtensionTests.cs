using System.Text;

namespace StrawberryShake.Transport.WebSockets.Protocols;

public class GraphQlWsSocketWriterExtensionTests
{
    [Fact]
    public async Task WriteStartOperationMessage_WithOperation_IsMatch()
    {
        // arrange
        await using var writer = new SocketMessageWriter();
        var operationId = "12f90cc5-2905-4d10-b33a-cb6d8f98a810";
        var request = new OperationRequest("Foo",
            GetHeroQueryDocument.Instance,
            new Dictionary<string, object?>() { { "Var1", "Value1" }, });

        // act
        writer.WriteStartOperationMessage(operationId, request);

        // assert
        Encoding.UTF8.GetString(writer.Body.Span).MatchSnapshot();
    }

    [Fact]
    public async Task WriteStartOperationMessage_OperationIdNull_IsMatch()
    {
        // arrange
        await using var writer = new SocketMessageWriter();
        var request = new OperationRequest("Foo",
            GetHeroQueryDocument.Instance,
            new Dictionary<string, object?>() { { "Var1", "Value1" }, });

        // act
        var ex =
            Record.Exception(() => writer.WriteStartOperationMessage(null!, request));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task WriteStartOperationMessage_RequestIsNull_IsMatch()
    {
        // arrange
        await using var writer = new SocketMessageWriter();
        var operationId = "12f90cc5-2905-4d10-b33a-cb6d8f98a810";

        // act
        var ex =
            Record.Exception(() => writer.WriteStartOperationMessage(operationId, null!));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task WriteStopOperationMessage_WithOperationId_IsMatch()
    {
        // arrange
        await using var writer = new SocketMessageWriter();
        var operationId = "12f90cc5-2905-4d10-b33a-cb6d8f98a810";

        // act
        writer.WriteStopOperationMessage(operationId);

        // assert
        Encoding.UTF8.GetString(writer.Body.Span).MatchSnapshot();
    }

    [Fact]
    public async Task WriteStopOperationMessage_OperationIdNull_IsMatch()
    {
        // arrange
        await using var writer = new SocketMessageWriter();

        // act
        var ex =
            Record.Exception(() => writer.WriteStopOperationMessage(null!));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task WriteInitializeMessage_Default_IsMatch()
    {
        // arrange
        await using var writer = new SocketMessageWriter();

        // act
        writer.WriteInitializeMessage(null);

        // assert
        Encoding.UTF8.GetString(writer.Body.Span).MatchSnapshot();
    }

    [Fact]
    public async Task WriteInitializeMessage_String_IsMatch()
    {
        // arrange
        await using var writer = new SocketMessageWriter();

        // act
        writer.WriteInitializeMessage("Payload");

        // assert
        Encoding.UTF8.GetString(writer.Body.Span).MatchSnapshot();
    }

    [Fact]
    public async Task WriteInitializeMessage_Dictionary_IsMatch()
    {
        // arrange
        await using var writer = new SocketMessageWriter();

        // act
        writer.WriteInitializeMessage(new Dictionary<string, object> { ["Key"] = "Value", });

        // assert
        Encoding.UTF8.GetString(writer.Body.Span).MatchSnapshot();
    }

    [Fact]
    public async Task WriteInitializeMessage_CustomObject_IsMatch()
    {
        // arrange
        await using var writer = new SocketMessageWriter();

        // act
        writer.WriteInitializeMessage(new CustomPayload());

        // assert
        Encoding.UTF8.GetString(writer.Body.Span).MatchSnapshot();
    }

    [Fact]
    public async Task WriteTerminateMessage_Default_IsMatch()
    {
        // arrange
        await using var writer = new SocketMessageWriter();

        // act
        writer.WriteTerminateMessage();

        // assert
        Encoding.UTF8.GetString(writer.Body.Span).MatchSnapshot();
    }

    private sealed class GetHeroQueryDocument : IDocument
    {
        private const string _bodyString =
            @"query GetHero {
                hero {
                    __typename
                    id
                    name
                    friends {
                        nodes {
                            __typename
                            id
                            name
                        }
                        totalCount
                    }
                }
                version
            }";

        private static readonly byte[] _body = Encoding.UTF8.GetBytes(_bodyString);

        private GetHeroQueryDocument() { }

        public OperationKind Kind => OperationKind.Query;

        public ReadOnlySpan<byte> Body => _body;

        public DocumentHash Hash { get; } = new("MD5", "ABC");

        public override string ToString() => _bodyString;

        public static GetHeroQueryDocument Instance { get; } = new();
    }

    private sealed class CustomPayload
    {
        public string Key { get; set; } = "Value";
    }
}
