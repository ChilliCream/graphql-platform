using System;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate
{
    public class SchemaErrorTests
    {
        [Fact]
        public void CreateSchemaError_TwoArguments_PopertiesAreSet()
        {
            // arrange
            var message = "FooBar";
            var exception = new Exception();

            // act
            var schemaError = new SchemaError(message, exception);

            // assert
            Assert.Equal(message, schemaError.Message);
            Assert.Equal(exception, schemaError.AssociatedException);
            Assert.Null(schemaError.SyntaxNode);
            Assert.Null(schemaError.Type);
        }

        [Fact]
        public void CreateSchemaError_TwoArgsNoMsg_ArgumentNullException()
        {
            // arrange
            var exception = new Exception();

            // act
            Action a = () => new SchemaError(null, exception);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void CreateSchemaError_ThreeArguments_PopertiesAreSet()
        {
            // arrange
            var message = "FooBar";
            var exception = new Exception();

            // act
            var schemaError = new SchemaError(
                message, new StringType(), exception);

            // assert
            Assert.Equal(message, schemaError.Message);
            Assert.Equal(exception, schemaError.AssociatedException);
            Assert.Null(schemaError.SyntaxNode);
            Assert.IsType<StringType>(schemaError.Type);
        }

        [Fact]
        public void CreateSchemaError_ThreeArgsNoMsg_ArgumentNullException()
        {
            // arrange
            var exception = new Exception();

            // act
            Action a = () => new SchemaError(null, new StringType(), exception);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void CreateSchemaError_FourArguments_PopertiesAreSet()
        {
            // arrange
            var message = "FooBar";
            var exception = new Exception();

            // act
            var schemaError = new SchemaError(
                message, new StringType(), new NameNode("foo"), exception);

            // assert
            Assert.Equal(message, schemaError.Message);
            Assert.Equal(exception, schemaError.AssociatedException);
            Assert.IsType<NameNode>(schemaError.SyntaxNode);
            Assert.IsType<StringType>(schemaError.Type);
        }

        [Fact]
        public void CreateSchemaError_FourArgs_ArgumentNullException()
        {
            // arrange
            var exception = new Exception();

            // act
            Action a = () => new SchemaError(
                null, new StringType(), new NameNode("foo"), exception);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }
    }
}
