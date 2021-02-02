using System;
using System.Buffers;
using System.Runtime.Serialization;
using System.Text;
using Snapshooter;
using Snapshooter.Xunit;
using StrawberryShake.Http.Subscriptions;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
    public class GraphQlWsMessageParserTests
    {
        [Fact]
        public void ParseMessage_EmptyObject_ThrowException()
        {
            // arrange
            ReadOnlySequence<byte> message = GetBytes("{}");

            // act
            Exception? ex = Record.Exception(() => GraphQlWsMessageParser.Parse(message));

            // assert
            Assert.IsType<SerializationException>(ex).Message.MatchSnapshot();
        }

        [Fact]
        public void ParseMessage_OnlyId_ThrowException()
        {
            // arrange
            ReadOnlySequence<byte> message = GetBytes(@"{""id"": ""123""}");

            // act
            Exception? ex = Record.Exception(() => GraphQlWsMessageParser.Parse(message));

            // assert
            Assert.IsType<SerializationException>(ex).Message.MatchSnapshot();
        }

        [Fact]
        public void ParseMessage_IncompleteDocument_ThrowException()
        {
            // arrange
            ReadOnlySequence<byte> message = GetBytes(@"{""id"": ""123""");

            // act
            Exception? ex = Record.Exception(() => GraphQlWsMessageParser.Parse(message));

            // assert
            Assert.NotNull(ex);
            ex.Message.MatchSnapshot();
        }

        [Fact]
        public void ParseMessage_AdditionalField_ThrowException()
        {
            // arrange
            ReadOnlySequence<byte> message = GetBytes(@"{""type"": ""ka"", ""Foo"":1}");

            // act
            Exception? ex = Record.Exception(() => GraphQlWsMessageParser.Parse(message));

            // assert
            Assert.IsType<SerializationException>(ex).Message.MatchSnapshot();
        }

        [Fact]
        public void ParseMessage_TypeIsNull_ThrowException()
        {
            // arrange
            ReadOnlySequence<byte> message = GetBytes($@"{{""type"": null, ""Foo"":1}}");

            // act
            Exception? ex = Record.Exception(() => GraphQlWsMessageParser.Parse(message));

            // assert
            Assert.IsType<SerializationException>(ex).Message.MatchSnapshot();
        }

        [Theory]
        [InlineData("ASDF")]
        [InlineData("ko")]
        [InlineData("do")]
        [InlineData("eo")]
        [InlineData("stat")]
        [InlineData("stot")]
        [InlineData("comp")]
        [InlineData("connection_ix")]
        [InlineData("connection_ax")]
        [InlineData("connection_ex")]
        [InlineData("connection_tx")]
        public void ParseMessage_UnknownType_ThrowException(string type)
        {
            // arrange
            ReadOnlySequence<byte> message = GetBytes($@"{{""type"": ""{type}"", ""Foo"":1}}");

            // act
            Exception? ex = Record.Exception(() => GraphQlWsMessageParser.Parse(message));

            // assert
            Assert.IsType<SerializationException>(ex)
                .Message.MatchSnapshot(new SnapshotNameExtension(type));
        }

        [Fact]
        public void ParseMessage_KeepAlive_ParseMessage()
        {
            // arrange
            ReadOnlySequence<byte> message = GetBytes(@"{""type"": ""ka""}");

            // act
            GraphQlWsMessage parsed = GraphQlWsMessageParser.Parse(message);

            // assert
            Assert.Equal(GraphQlWsMessageType.KeepAlive, parsed.Type);
        }

        [Fact]
        public void ParseMessage_Data_ParseMessage()
        {
            // arrange
            ReadOnlySequence<byte> message =
                GetBytes(@"{""type"": ""data"", ""id"":""123"", ""payload"": ""payload""}");

            // act
            GraphQlWsMessage parsed = GraphQlWsMessageParser.Parse(message);

            // assert
            Assert.Equal(GraphQlWsMessageType.Data, parsed.Type);
            Assert.Equal("123", parsed.Id);
            Assert.Equal(@"""payload""", Encoding.UTF8.GetString(parsed.Payload.FirstSpan));
        }

        [Fact]
        public void ParseMessage_Error_ParseMessage()
        {
            // arrange
            ReadOnlySequence<byte> message =
                GetBytes(@"{""type"": ""error"", ""id"":""123"", ""payload"": ""payload""}");

            // act
            GraphQlWsMessage parsed = GraphQlWsMessageParser.Parse(message);

            // assert
            Assert.Equal(GraphQlWsMessageType.Error, parsed.Type);
            Assert.Equal("123", parsed.Id);
            Assert.Equal(@"""payload""", Encoding.UTF8.GetString(parsed.Payload.FirstSpan));
        }

        [Fact]
        public void ParseMessage_Start_ParseMessage()
        {
            // arrange
            ReadOnlySequence<byte> message =
                GetBytes(@"{""type"": ""start"", ""id"":""123"", ""payload"": ""payload""}");

            // act
            GraphQlWsMessage parsed = GraphQlWsMessageParser.Parse(message);

            // assert
            Assert.Equal(GraphQlWsMessageType.Start, parsed.Type);
            Assert.Equal("123", parsed.Id);
            Assert.Equal(@"""payload""", Encoding.UTF8.GetString(parsed.Payload.FirstSpan));
        }

        [Fact]
        public void ParseMessage_Stop_ParseMessage()
        {
            // arrange
            ReadOnlySequence<byte> message =
                GetBytes(@"{""type"": ""stop"", ""id"":""123""}");

            // act
            GraphQlWsMessage parsed = GraphQlWsMessageParser.Parse(message);

            // assert
            Assert.Equal(GraphQlWsMessageType.Stop, parsed.Type);
            Assert.Equal("123", parsed.Id);
        }

        [Fact]
        public void ParseMessage_Complete_ParseMessage()
        {
            // arrange
            ReadOnlySequence<byte> message =
                GetBytes(@"{""type"": ""complete"", ""id"":""123""}");

            // act
            GraphQlWsMessage parsed = GraphQlWsMessageParser.Parse(message);

            // assert
            Assert.Equal(GraphQlWsMessageType.Complete, parsed.Type);
            Assert.Equal("123", parsed.Id);
        }

        [Fact]
        public void ParseMessage_ConnectionInit_ParseMessage()
        {
            // arrange
            ReadOnlySequence<byte> message =
                GetBytes(@"{""type"": ""connection_init""}");

            // act
            GraphQlWsMessage parsed = GraphQlWsMessageParser.Parse(message);

            // assert
            Assert.Equal(GraphQlWsMessageType.ConnectionInit, parsed.Type);
        }

        [Fact]
        public void ParseMessage_ConnectionAccept_ParseMessage()
        {
            // arrange
            ReadOnlySequence<byte> message =
                GetBytes(@"{""type"": ""connection_ack""}");

            // act
            GraphQlWsMessage parsed = GraphQlWsMessageParser.Parse(message);

            // assert
            Assert.Equal(GraphQlWsMessageType.ConnectionAccept, parsed.Type);
        }

        [Fact]
        public void ParseMessage_ConnectionError_ParseMessage()
        {
            // arrange
            ReadOnlySequence<byte> message =
                GetBytes(@"{""type"": ""connection_error"", ""payload"": ""payload""}");

            // act
            GraphQlWsMessage parsed = GraphQlWsMessageParser.Parse(message);

            // assert
            Assert.Equal(GraphQlWsMessageType.ConnectionError, parsed.Type);
            Assert.Equal(@"""payload""", Encoding.UTF8.GetString(parsed.Payload.FirstSpan));
        }

        [Fact]
        public void ParseMessage_ConnectionTerminate_ParseMessage()
        {
            // arrange
            ReadOnlySequence<byte> message =
                GetBytes(@"{""type"": ""connection_terminate"" }");

            // act
            GraphQlWsMessage parsed = GraphQlWsMessageParser.Parse(message);

            // assert
            Assert.Equal(GraphQlWsMessageType.ConnectionTerminate, parsed.Type);
        }

        private ReadOnlySequence<byte> GetBytes(string message)
        {
            return new(Encoding.UTF8.GetBytes(message));
        }
    }
}
