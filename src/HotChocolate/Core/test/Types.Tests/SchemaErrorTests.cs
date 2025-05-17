using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate;

public class SchemaErrorTests
{
    [Fact]
    public void CreateSchemaError_ExceptionAndMessage()
    {
        // arrange
        var message = "FooBar";
        var exception = new Exception();

        // act
        var schemaError = SchemaErrorBuilder.New()
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
        var schemaError = SchemaErrorBuilder.New()
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
        var schemaError = SchemaErrorBuilder.New()
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
        var schemaError = SchemaErrorBuilder.New()
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
        var schemaError = SchemaErrorBuilder.New()
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
            .TryAddTypeInterceptor(errorInterceptor)
            .Create();

        // assert
        Assert.Throws<SchemaException>(Action);
        Assert.Collection(
            errorInterceptor.Exceptions,
            ex => Assert.IsType<SchemaException>(ex));
    }

    [Fact]
    public void IncorrectType_In_Parameters_ShouldThrow()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"type Query {
                    test(bar: Input123): String
                }")
            .Use(_ => _ => default);

        // act
        var ex = Assert.Throws<SchemaException>(() => schema.Create());

        // assert
        Assert.Equal(2, ex.Errors.Count);
        ex.Errors.First().Message.MatchSnapshot();
    }

    private sealed class ErrorInterceptor : TypeInterceptor
    {
        public List<Exception> Exceptions { get; } = [];

        public override void OnCreateSchemaError(IDescriptorContext context, Exception error)
            => Exceptions.Add(error);
    }
}
