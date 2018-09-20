
using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class InputObjectDeserializerTests
    {
        [Fact]
        public void SerializeScalar()
        {
            // arrange
            IntType sourceType = new IntType();
            Type targetType = typeof(int);
            IntValueNode literal = new IntValueNode("1");

            // act
            object result = InputObjectDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<int>(result);
            Assert.Equal(1, result);
        }

        [Fact]
        public void SerializeScalarAndConvert()
        {
            // arrange
            var sourceType = new IntType();
            var targetType = typeof(string);
            var literal = new IntValueNode("1");

            // act
            object result = InputObjectDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<string>(result);
            Assert.Equal("1", result);
        }

        [Fact]
        public void SerializeNonNullScalar()
        {
            // arrange
            var sourceType = new NonNullType(new IntType());
            var targetType = typeof(int);
            IntValueNode literal = new IntValueNode("1");

            // act
            object result = InputObjectDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<int>(result);
            Assert.Equal(1, result);
        }

        [Fact]
        public void SerializeScalarNullValue()
        {
            // arrange
            var sourceType = new IntType();
            var targetType = typeof(int);
            NullValueNode literal = NullValueNode.Default;

            // act
            object result = InputObjectDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void SerializeScalarListToArray()
        {
            // arrange
            var sourceType = new ListType(new IntType());
            var targetType = typeof(int);
            var literal = new ListValueNode(new IntValueNode("1"));

            // act
            object result = InputObjectDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<int>(result);
            Assert.Equal(1, result);
        }


        private static ISchema CreateSchema()
        {
            return Schema.Create(c =>
            {

            });
        }



    }
}
