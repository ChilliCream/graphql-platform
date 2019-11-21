using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using Snapshooter.Xunit;
using StrawberryShake.Serializers;
using StrawberryShake.Transport;
using Xunit;

namespace StrawberryShake.Http
{
    public class JsonOperationFormatterTests
    {
        [Fact]
        public void Serialize_To_Json_With_Document()
        {
            // arrange
            var serializerResolver = new Mock<IValueSerializerResolver>();
            var formatter = new JsonOperationFormatter(serializerResolver.Object);
            var formatterOptions = new OperationFormatterOptions(includeDocument: true);
            var operation = new Operation();
            var messageWriter = new MessageWriter();

            // act
            formatter.Serialize(operation, messageWriter, formatterOptions);

            // assert
            Encoding.UTF8.GetString(messageWriter.Body.Span).MatchSnapshot();
        }

        [Fact]
        public void Serialize_To_Json_Without_Document()
        {
            // arrange
            var serializerResolver = new Mock<IValueSerializerResolver>();
            var formatter = new JsonOperationFormatter(serializerResolver.Object);
            var formatterOptions = new OperationFormatterOptions(includeDocument: false);
            var operation = new Operation();
            var messageWriter = new MessageWriter();

            // act
            formatter.Serialize(operation, messageWriter, formatterOptions);

            // assert
            Encoding.UTF8.GetString(messageWriter.Body.Span).MatchSnapshot();
        }

        [Fact]
        public void Serialize_To_Json_With_Id()
        {
            // arrange
            var serializerResolver = new Mock<IValueSerializerResolver>();
            var formatter = new JsonOperationFormatter(serializerResolver.Object);
            var formatterOptions = new OperationFormatterOptions(includeId: true);
            var operation = new Operation();
            var messageWriter = new MessageWriter();

            // act
            formatter.Serialize(operation, messageWriter, formatterOptions);

            // assert
            Encoding.UTF8.GetString(messageWriter.Body.Span).MatchSnapshot();
        }

        [Fact]
        public void Serialize_To_Json_Without_Id()
        {
            // arrange
            var serializerResolver = new Mock<IValueSerializerResolver>();
            var formatter = new JsonOperationFormatter(serializerResolver.Object);
            var formatterOptions = new OperationFormatterOptions(includeId: false);
            var operation = new Operation();
            var messageWriter = new MessageWriter();

            // act
            formatter.Serialize(operation, messageWriter, formatterOptions);

            // assert
            Encoding.UTF8.GetString(messageWriter.Body.Span).MatchSnapshot();
        }

        [Fact]
        public void Serialize_To_Json_With_Extensions()
        {
            // arrange
            var serializerResolver = new Mock<IValueSerializerResolver>();
            var formatter = new JsonOperationFormatter(serializerResolver.Object);
            var formatterOptions = new OperationFormatterOptions(
                new Dictionary<string, object?>
                {
                    { "key", "value" }
                });
            var operation = new Operation();
            var messageWriter = new MessageWriter();

            // act
            formatter.Serialize(operation, messageWriter, formatterOptions);

            // assert
            Encoding.UTF8.GetString(messageWriter.Body.Span).MatchSnapshot();
        }

        [Fact]
        public void Serialize_To_Json_With_Variables()
        {
            // arrange
            var serializerResolver = new Mock<IValueSerializerResolver>();
            serializerResolver.Setup(t => t.GetValueSerializer(It.IsAny<string>()))
                .Returns(new StringValueSerializer());
            var formatter = new JsonOperationFormatter(serializerResolver.Object);
            var formatterOptions = new OperationFormatterOptions();
            var operation = new OperationWithVariables();
            var messageWriter = new MessageWriter();

            // act
            formatter.Serialize(operation, messageWriter, formatterOptions);

            // assert
            Encoding.UTF8.GetString(messageWriter.Body.Span).MatchSnapshot();
        }

        private class Operation
            : IOperation
        {
            public string Name => "abc";

            public IDocument Document { get; } = new Document();

            public OperationKind Kind { get; } = OperationKind.Subscription;

            public Type ResultType => typeof(OnReview);

            public IReadOnlyList<VariableValue> GetVariableValues() =>
                Array.Empty<VariableValue>();
        }

        private class OperationWithVariables
            : IOperation
        {
            public string Name => "abc";

            public IDocument Document { get; } = new Document();

            public OperationKind Kind { get; } = OperationKind.Subscription;

            public Type ResultType => typeof(OnReview);

            public IReadOnlyList<VariableValue> GetVariableValues() =>
                new[] { new VariableValue("Foo", "Bar", "123") };
        }

        private class Document
            : IDocument
        {
            public ReadOnlySpan<byte> HashName => Encoding.UTF8.GetBytes("HashName");

            public ReadOnlySpan<byte> Hash => Encoding.UTF8.GetBytes("Hash");

            public ReadOnlySpan<byte> Content => Encoding.UTF8.GetBytes(
                "subscription abc { onReview(episode: NEWHOPE) { stars } }");
        }

        public class OnReview
        {
            public int Stars { get; set; }
        }
    }
}
