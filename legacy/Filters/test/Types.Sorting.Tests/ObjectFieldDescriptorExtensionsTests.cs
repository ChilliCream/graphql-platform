using System;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using Moq;
using Xunit;

namespace HotChocolate.Types.Sorting;

[Obsolete]
public class ObjectFieldDescriptorExtensionsTests
    : DescriptorTestBase
{
    [Fact]
    public void UseSorting_WithoutParams_ShouldRegisterPlaceholderMiddleware()
    {
        // arrange
        var descriptor = ObjectFieldDescriptor.New(Context, "field");
        descriptor.Resolve("abc");

        // act
        descriptor.UseSorting();

        // assert
        Assert.Single(descriptor.CreateDefinition().MiddlewareDefinitions);
    }

    [Fact]
    public void UseSorting_WithoutTypeParam_ShouldRegisterPlaceholderMiddleware()
    {
        // arrange
        var descriptor = ObjectFieldDescriptor.New(Context, "field");

        // act
        descriptor.UseSorting<object>();

        // assert
        Assert.Single(descriptor.CreateDefinition().MiddlewareDefinitions);
    }

    [Fact]
    public void UseSorting_FieldDescConfig_FieldDesc_Is_Null()
    {
        // arrange
        Action<ISortInputTypeDescriptor<object>> config = d => { };

        // act
        Action action = () =>
            SortObjectFieldDescriptorExtensions
                .UseSorting(null!, config);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void UseSorting_FieldDescConfig_Config_Is_Null()
    {
        // arrange
        var desc = Mock.Of<IObjectFieldDescriptor>();

        // act
        Action action = () =>
            SortObjectFieldDescriptorExtensions
                .UseSorting<object>(desc, null);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }
}