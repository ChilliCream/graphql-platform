using System.Text.Json;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets;

public class OperationMessagesTest
{
    [Fact]
    public void CancelledOperationMessage_Default_IsMatch()
    {
        // arrange
        // act
        // assert
        CancelledOperationMessage.Default.MatchSnapshot();
    }

    [Fact]
    public void CompleteOperationMessage_Default_IsMatch()
    {
        // arrange
        // act
        // assert
        CompleteOperationMessage.Default.MatchSnapshot();
    }

    [Fact]
    public void ErrorOperationMessage_WithMessage_IsMatch()
    {
        // arrange
        var message = "Foo";

        // act
        var operationMessage = new ErrorOperationMessage(message);

        // assert
        operationMessage.MatchSnapshot();
    }

    [Fact]
    public void ErrorOperationMessage_MessageIsNull_Throw()
    {
        // arrange
        string message = null!;

        // act
        var ex = Record.Exception(() => new ErrorOperationMessage(message));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void ErrorOperationMessage_ConnectionError_IsMatch()
    {
        // arrange
        // act
        // assert
        ErrorOperationMessage.ConnectionInitializationError.MatchSnapshot();
    }

    [Fact]
    public void JsonDocumentOperationMessage_FromBytes_IsMatch()
    {
        // arrange
        var message = JsonDocument.Parse(@"{ ""Foo"": ""Bar""}");

        // act
        OperationMessage operationMessage =
            new DataDocumentOperationMessage<JsonDocument>(message);

        // assert
        operationMessage.MatchSnapshot();
    }
}
