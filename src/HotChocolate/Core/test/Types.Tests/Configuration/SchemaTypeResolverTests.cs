using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

public class SchemaTypeResolverTests
{
    private readonly ITypeInspector _typeInspector = new DefaultTypeInspector();

    private IExtendedType TypeOf<T>() =>
        _typeInspector.GetType(typeof(T));

    [InlineData(TypeContext.Output)]
    [InlineData(TypeContext.None)]
    [Theory]
    public void InferObjectType(TypeContext context)
    {
        // arrange
        var descriptorContext = DescriptorContext.Create();
        var typeReference = TypeReference.Create(TypeOf<Bar>(), context);

        // act
        var success = descriptorContext.TryInferSchemaType(typeReference, out var schemaTypes);

        // assert
        Assert.True(success);
        Assert.Collection(schemaTypes,
            type =>
            {
                Assert.Equal(TypeContext.Output, type.Context);
                Assert.Equal(typeof(ObjectType<Bar>), ((ExtendedTypeReference)type).Type.Source);
            });
    }

    [InlineData(TypeContext.Output)]
    [InlineData(TypeContext.None)]
    [Theory]
    public void InferObjectTypeFromStruct(TypeContext context)
    {
        // arrange
        var descriptorContext = DescriptorContext.Create();
        var typeReference = TypeReference.Create(TypeOf<BarStruct>(), context);

        // act
        var success = descriptorContext.TryInferSchemaType(typeReference, out var schemaTypes);

        // assert
        Assert.True(success);
        Assert.Collection(schemaTypes,
            type =>
            {
                Assert.Equal(TypeContext.Output, type.Context);
                Assert.Equal(typeof(ObjectType<BarStruct>), ((ExtendedTypeReference)type).Type.Source);
            });
    }

    [InlineData(TypeContext.Output)]
    [InlineData(TypeContext.None)]
    [Theory]
    public void RejectRefStructAsObjectType(TypeContext context)
    {
        // arrange
        var descriptorContext = DescriptorContext.Create();
        var typeReference = TypeReference.Create(
            _typeInspector.GetType(typeof(BarRefStruct)),
            context);

        // act
        var success = descriptorContext.TryInferSchemaType(typeReference, out var schemaTypes);

        // assert
        Assert.False(success);
        Assert.Null(schemaTypes);
    }

    [InlineData(TypeContext.Output)]
    [InlineData(TypeContext.None)]
    [Theory]
    public void InferInterfaceType(TypeContext context)
    {
        // arrange
        var descriptorContext = DescriptorContext.Create();
        var typeReference = TypeReference.Create(TypeOf<IBar>(), context);

        // act
        var success = descriptorContext.TryInferSchemaType(typeReference, out var schemaTypes);

        // assert
        Assert.True(success);
        Assert.Collection(schemaTypes,
            type =>
            {
                Assert.Equal(TypeContext.Output, type.Context);
                Assert.Equal(typeof(InterfaceType<IBar>), ((ExtendedTypeReference)type).Type.Source);
            });
    }

    [Fact]
    public void InferInputObjectType()
    {
        // arrange
        var descriptorContext = DescriptorContext.Create();
        var typeReference = TypeReference.Create(TypeOf<Bar>(), TypeContext.Input);

        // act
        var success = descriptorContext.TryInferSchemaType(typeReference, out var schemaTypes);

        // assert
        Assert.True(success);
        Assert.Collection(schemaTypes,
            type =>
            {
                Assert.Equal(TypeContext.Input, type.Context);
                Assert.Equal(
                    typeof(InputObjectType<Bar>),
                    ((ExtendedTypeReference)type).Type.Source);
            });
    }

    [InlineData(TypeContext.Output)]
    [InlineData(TypeContext.Input)]
    [InlineData(TypeContext.None)]
    [Theory]
    public void InferEnumType(TypeContext context)
    {
        // arrange
        var descriptorContext = DescriptorContext.Create();
        var typeReference = TypeReference.Create(TypeOf<Foo>(), context);

        // act
        var success = descriptorContext.TryInferSchemaType(typeReference, out var schemaTypes);

        // assert
        Assert.True(success);
        Assert.Collection(
            schemaTypes,
            type =>
            {
                Assert.Equal(TypeContext.None, type.Context);
                Assert.Equal(typeof(EnumType<Foo>), ((ExtendedTypeReference)type).Type.Source);
            });
    }

    public class Bar
    {
        public string Baz { get; }
    }

    public struct BarStruct
    {
        public string Baz { get; }
    }

    public ref struct BarRefStruct
    {
        public string Baz { get; }
    }

    public interface IBar
    {
        string Baz { get; }
    }

    public enum Foo
    {
        Bar,
        Baz,
    }
}
