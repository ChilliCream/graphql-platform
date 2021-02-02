using System;
using System.Buffers;
using System.Text;
using Snapshooter.Xunit;
using StrawberryShake.Transport.WebSockets.Messages;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
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
        public void ErrorOperationMessage_UnexpectedServerError_IsMatch()
        {
            // arrange
            // act
            // assert
            ErrorOperationMessage.UnexpectedServerError.MatchSnapshot();
        }

        [Fact]
        public void ErrorOperationMessage_ConnectionError_IsMatch()
        {
            // arrange
            // act
            // assert
            ErrorOperationMessage.ConnectionError.MatchSnapshot();
        }

        [Fact]
        public void JsonDocumentOperationMessage_FromBytes_IsMatch()
        {
            // arrange
            var message =
                new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(@"{ ""Foo"": ""Bar""}"));

            // act
            OperationMessage operationMessage = new DataDocumentOperationMessage(message.First);

            // assert
            operationMessage.MatchSnapshot();
        }
    }
}
