using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Types;

public class ScalarTypeInputValueToLiteralTests
{
    [Fact]
    public void InputValueToLiteral_Should_StoreMemoryBuilderOnContext_When_ParserCreatesIt()
    {
        // arrange
        var type = new IntType();
        using var document = JsonDocument.Parse("42");
        var context = new FeatureProviderStub();

        // act
        var literal = type.InputValueToLiteral(document.RootElement, context);

        // assert
        Assert.Equal("42", Assert.IsType<IntValueNode>(literal).ToString());
        Assert.IsType<Utf8MemoryBuilder>(Assert.Single(context.Features).Value);
    }

    [Fact]
    public void InputValueToLiteral_Should_ReuseMemoryBuilder_When_ContextHasOne()
    {
        // arrange
        var type = new IntType();
        using var document = JsonDocument.Parse("[1, 2]");
        var context = new FeatureProviderStub();
        type.InputValueToLiteral(document.RootElement[0], context);
        var builder = context.Features.Get<Utf8MemoryBuilder>();

        // act
        var literal = type.InputValueToLiteral(document.RootElement[1], context);

        // assert
        Assert.Equal("2", Assert.IsType<IntValueNode>(literal).ToString());
        Assert.Same(builder, context.Features.Get<Utf8MemoryBuilder>());
    }

    private sealed class FeatureProviderStub : IFeatureProvider
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
    }
}
