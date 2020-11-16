using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate
{
    public class SchemaErrorTests
    {
        [Fact]
        public void CreateSchemaError_ExceptionAndMessage()
        {
            // arrange
            var message = "FooBar";
            var exception = new Exception();

            // act
            ISchemaError schemaError = SchemaErrorBuilder.New()
                .SetMessage(message)
                .SetException(exception)
                .Build();

            // assert
            Assert.Equal(message, schemaError.Message);
            Assert.Equal(exception, schemaError.Exception);
            Assert.Empty(schemaError.SyntaxNodes);
            Assert.Empty(schemaError.Extensions);
            Assert.Null(schemaError.TypeSystemObject);
            Assert.Null(schemaError.Path);
            Assert.Null(schemaError.Code);
        }

        [Fact]
        public void CreateSchemaError_Exception()
        {
            // arrange
            var exception = new Exception("FooBar");

            // act
            ISchemaError schemaError = SchemaErrorBuilder.New()
                .SetException(exception)
                .Build();

            // assert
            Assert.Equal(exception.Message, schemaError.Message);
            Assert.Equal(exception, schemaError.Exception);
            Assert.Empty(schemaError.SyntaxNodes);
            Assert.Empty(schemaError.Extensions);
            Assert.Null(schemaError.TypeSystemObject);
            Assert.Null(schemaError.Path);
            Assert.Null(schemaError.Code);
        }

        [Fact]
        public void CreateSchemaError_ThreeArguments_PopertiesAreSet()
        {
            // arrange
            var message = "FooBar";
            var exception = new Exception();
            var type = new StringType();

            // act
            ISchemaError schemaError = SchemaErrorBuilder.New()
                .SetMessage(message)
                .SetException(exception)
                .SetTypeSystemObject(type)
                .Build();

            // assert
            Assert.Equal(message, schemaError.Message);
            Assert.Equal(exception, schemaError.Exception);
            Assert.Equal(type, schemaError.TypeSystemObject);
            Assert.Empty(schemaError.SyntaxNodes);
            Assert.Empty(schemaError.Extensions);
            Assert.Null(schemaError.Path);
            Assert.Null(schemaError.Code);
        }

        [Fact]
        public void CreateSchemaError_SetExtension()
        {
            // arrange
            var message = "FooBar";
            var key = "foo";
            var value = "bar";

            // act
            ISchemaError schemaError = SchemaErrorBuilder.New()
                .SetMessage(message)
                .SetExtension(key, value)
                .Build();

            // assert
            Assert.Equal(message, schemaError.Message);
            Assert.Empty(schemaError.SyntaxNodes);
            Assert.Collection(schemaError.Extensions,
                t =>
                {
                    Assert.Equal(key, t.Key);
                    Assert.Equal(value, t.Value);
                });
            Assert.Null(schemaError.Exception);
            Assert.Null(schemaError.TypeSystemObject);
            Assert.Null(schemaError.Path);
            Assert.Null(schemaError.Code);
        }

        [Fact]
        public void CreateSchemaError_AddSyntaxNode()
        {
            // arrange
            var message = "FooBar";
            var node = new NameNode("foo");


            // act
            ISchemaError schemaError = SchemaErrorBuilder.New()
                .SetMessage(message)
                .AddSyntaxNode(node)
                .Build();

            // assert
            Assert.Equal(message, schemaError.Message);
            Assert.Collection(schemaError.SyntaxNodes,
                t => Assert.Equal(node, t));
            Assert.Empty(schemaError.Extensions);
            Assert.Null(schemaError.Exception);
            Assert.Null(schemaError.TypeSystemObject);
            Assert.Null(schemaError.Path);
            Assert.Null(schemaError.Code);
        }

        [Fact]
        public void Intercept_Schema_Error()
        {
            // arrange
            var errorInterceptor = new ErrorInterceptor();

            // act
            void Action() => SchemaBuilder.New()
                .TryAddSchemaInterceptor(errorInterceptor)
                .Create();

            // assert
            Assert.Throws<SchemaException>(Action);
            Assert.Collection(
                errorInterceptor.Exceptions,
                ex => Assert.IsType<SchemaException>(ex));
        }

        private class ErrorInterceptor : SchemaInterceptor
        {
            public List<Exception> Exceptions { get; } = new List<Exception>();

            public override void OnError(IDescriptorContext context, Exception exception)
            {
                Exceptions.Add(exception);
            }
        }
    }
}
