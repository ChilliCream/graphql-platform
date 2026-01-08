using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.Types;

public class ScalarTypeTestBase
{
    protected Schema BuildSchema<TType>()
        where TType : ScalarType
    {
        return SchemaBuilder
            .New()
            .AddQueryType(x => x.Name("Query").Field("scalar").Type<TType>().Resolve(""))
            .Create();
    }

    protected ScalarType CreateType<TType>()
        where TType : ScalarType
    {
        return BuildSchema<TType>().Types.GetType<ObjectType>("Query").Fields["scalar"].Type
            as ScalarType ?? throw new InvalidOperationException();
    }

    protected IValueNode CreateValueNode(Type type, object? value)
    {
        switch (type.Name)
        {
            case nameof(BooleanValueNode) when value is bool b:
                return new BooleanValueNode(b);
            case nameof(EnumValueNode) when value is Enum e:
                return new EnumValueNode(e);
            case nameof(FloatValueNode) when value is double d:
                return new FloatValueNode(d);
            case nameof(FloatValueNode) when value is decimal d:
                return new FloatValueNode(d);
            case nameof(IntValueNode) when value is int i:
                return new IntValueNode(i);
            case nameof(IntValueNode) when value is uint i:
                return new IntValueNode(i);
            case nameof(IntValueNode) when value is ulong i:
                return new IntValueNode(i);
            case nameof(IntValueNode) when value is ushort i:
                return new IntValueNode(i);
            case nameof(IntValueNode) when value is sbyte i:
                return new IntValueNode(i);
            case nameof(NullValueNode):
                return NullValueNode.Default;
            case nameof(StringValueNode) when value is string s:
                return new StringValueNode(s);
            default:
                throw new InvalidOperationException();
        }
    }

    protected void ExpectIsInstanceOfTypeToMatch<TType>(
        IValueNode valueSyntax,
        bool expectedResult)
        where TType : ScalarType
    {
        // arrange
        var scalar = CreateType<TType>();

        // act
        var result = scalar.IsValueCompatible(valueSyntax);

        // assert
        Assert.Equal(expectedResult, result);
    }

    protected void ExpectParseLiteralToMatch<TType>(
        IValueNode valueSyntax,
        object? expectedResult)
        where TType : ScalarType
    {
        // arrange
        var scalar = CreateType<TType>();

        // act
        var result = scalar.CoerceInputLiteral(valueSyntax);

        // assert
        Assert.Equal(expectedResult, result);
    }

    protected void ExpectParseLiteralToThrowSerializationException<TType>(
        IValueNode valueSyntax)
        where TType : ScalarType
    {
        // arrange
        var scalar = CreateType<TType>();

        // act
        var result = Record.Exception(() => scalar.CoerceInputLiteral(valueSyntax));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    protected void ExpectValueToLiteralToMatchType<TType>(
        object? runtimeValue,
        Type type)
        where TType : ScalarType
    {
        // arrange
        var scalar = CreateType<TType>();

        // act
        var result = scalar.ValueToLiteral(runtimeValue!);

        // assert
        Assert.Equal(type, result.GetType());
    }

    protected void ExpectValueToLiteralToThrowSerializationException<TType>(object? runtimeValue)
        where TType : ScalarType
    {
        // arrange
        var scalar = CreateType<TType>();

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(runtimeValue!));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    protected void ExpectCoerceOutputValueToMatch<TType>(
        object? runtimeValue)
        where TType : ScalarType
    {
        // arrange
        var scalar = CreateType<TType>();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        scalar.CoerceOutputValue(runtimeValue!, resultElement);

        // assert
        resultElement.MatchSnapshot();
    }

    protected void ExpectCoerceInputValueToMatch<TType>(
        string jsonValue,
        object? runtimeValue)
        where TType : ScalarType
    {
        // arrange
        var scalar = CreateType<TType>();
        var inputValue = JsonDocument.Parse(jsonValue).RootElement;

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var result = scalar.CoerceInputValue(inputValue, context.Object);

        // assert
        Assert.Equal(result, runtimeValue);
    }

    protected void ExpectCoerceOutputValueToThrowSerializationException<TType>(object runtimeValue)
        where TType : ScalarType
    {
        // arrange
        var scalar = CreateType<TType>();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        var result = Record.Exception(() => scalar.CoerceOutputValue(runtimeValue, resultElement));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    protected void ExpectCoerceInputValueToThrowSerializationException<TType>(string jsonValue)
        where TType : ScalarType
    {
        // arrange
        var scalar = CreateType<TType>();
        var inputValue = JsonDocument.Parse(jsonValue).RootElement;

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var result = Record.Exception(() => scalar.CoerceInputValue(inputValue, context.Object));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    protected async Task ExpectScalarTypeToBoundImplicityWhenRegistered<TType, TDefaultClass>()
        where TType : ScalarType
        where TDefaultClass : class
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<TDefaultClass>()
            .AddType<TType>()
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.ToString().MatchSnapshot();
    }

    public enum TestEnum
    {
        Foo
    }
}
