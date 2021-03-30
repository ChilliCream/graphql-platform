using System;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using Moq;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    [Obsolete]
    public class ObjectFieldDescriptorExtensionsTests
        : DescriptorTestBase
    {
        [Fact]
        public void UseSorting_WithoutParams_ShouldRegisterPlaceholderMiddleware()
        {
            // arrange
            ObjectFieldDescriptor descriptor = ObjectFieldDescriptor.New(Context, "field");
            descriptor.Resolve("abc");

            // act
            descriptor.UseSorting();

            // assert
            Assert.Single(descriptor.CreateDefinition().MiddlewareComponents);
        }

        [Fact]
        public void UseSorting_WithoutTypeParam_ShouldRegisterPlaceholderMiddleware()
        {
            // arrange
            ObjectFieldDescriptor descriptor =
                ObjectFieldDescriptor.New(Context, "field");
            FieldMiddleware placeholder = next => context => default;

            // act
            descriptor.UseSorting<object>();

            // assert
            Assert.Single(descriptor.CreateDefinition().MiddlewareComponents);
        }

        [Fact]
        public void UseSorting_FieldDescConfig_FieldDesc_Is_Null()
        {
            // arrange
            Action<ISortInputTypeDescriptor<object>> config = d => { };

            // act
            Action action = () =>
                SortObjectFieldDescriptorExtensions
                    .UseSorting<object>(null, config);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseSorting_FieldDescConfig_Config_Is_Null()
        {
            // arrange
            IObjectFieldDescriptor desc = Mock.Of<IObjectFieldDescriptor>();

            // act
            Action action = () =>
                SortObjectFieldDescriptorExtensions
                    .UseSorting<object>(desc, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
