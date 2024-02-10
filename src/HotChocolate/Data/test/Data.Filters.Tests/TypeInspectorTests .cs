using HotChocolate.Data.Filters;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Tests;

public class TypeInspectorTests
{
    [Fact]
    public void FilterInputType_Should_BeASchemaType_When_Inferred()
    {
        // arrange
        var inspector = new DefaultTypeInspector();

        // act
        var extendedType = inspector.GetType(typeof(FilterInputType<Foo>));

        // assert
        Assert.True(extendedType.IsSchemaType);
    }

    [Fact]
    public void FilterInputType_Should_BeASchemaType_When_NonGeneric()
    {
        // arrange
        var inspector = new DefaultTypeInspector();

        // act
        var extendedType = inspector.GetType(typeof(NonGenericType));

        // assert
        Assert.True(extendedType.IsSchemaType);
    }

    [Fact]
    public void FilterInputType_Should_BeASchemaType_When_Generic()
    {
        // arrange
        var inspector = new DefaultTypeInspector();

        // act
        var extendedType = inspector.GetType(typeof(GenericType));

        // assert
        Assert.True(extendedType.IsSchemaType);
    }

    [Fact]
    public void FilterInputType_Should_BeASchemaType_When_List()
    {
        // arrange
        var inspector = new DefaultTypeInspector();

        // act
        var extendedType =
            inspector.GetType(typeof(ListFilterInputType<FilterInputType<Foo>>));

        // assert
        Assert.True(extendedType.IsSchemaType);
        var typeArgument = Assert.Single(extendedType.TypeArguments);
        Assert.NotNull(typeArgument);
        Assert.True(typeArgument!.IsSchemaType);
    }

    private sealed class NonGenericType : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Field("test").Type<StringOperationFilterInputType>();
        }
    }

    private sealed class GenericType : FilterInputType<Foo>
    {
    }

    private sealed class Foo
    {
        public string? Test { get; set; }
    }
}
