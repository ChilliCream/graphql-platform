using HotChocolate.Data.Filters;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate.Data.Tests
{
    public class TypeInspectorTests
    {
        [Fact]
        public void FilterInputType_Should_BeASchemaType_When_Inferred()
        {
            // arrange
            DefaultTypeInspector inspector = new DefaultTypeInspector();

            // act
            IExtendedType extendedType = inspector.GetType(typeof(FilterInputType<Foo>));

            // assert
            Assert.True(extendedType.IsSchemaType);
        }

        [Fact]
        public void FilterInputType_Should_BeASchemaType_When_NonGeneric()
        {
            // arrange
            DefaultTypeInspector inspector = new DefaultTypeInspector();

            // act
            IExtendedType extendedType = inspector.GetType(typeof(NonGenericType));

            // assert
            Assert.True(extendedType.IsSchemaType);
        }

        [Fact]
        public void FilterInputType_Should_BeASchemaType_When_Generic()
        {
            // arrange
            DefaultTypeInspector inspector = new DefaultTypeInspector();

            // act
            IExtendedType extendedType = inspector.GetType(typeof(GenericType));

            // assert
            Assert.True(extendedType.IsSchemaType);
        }

        [Fact]
        public void FilterInputType_Should_BeASchemaType_When_List()
        {
            // arrange
            DefaultTypeInspector inspector = new DefaultTypeInspector();

            // act
            IExtendedType extendedType =
                inspector.GetType(typeof(ListFilterInput<FilterInputType<Foo>>));

            // assert
            Assert.True(extendedType.IsSchemaType);
            IExtendedType? typeArgument = Assert.Single(extendedType.TypeArguments);
            Assert.NotNull(typeArgument);
            Assert.True(typeArgument!.IsSchemaType);
        }

        private class NonGenericType : FilterInputType
        {
            protected override void Configure(IFilterInputTypeDescriptor descriptor)
            {
                descriptor.Field("test").Type<StringOperationFilterInput>();
            }
        }

        private class GenericType : FilterInputType<Foo>
        {
        }

        private class Foo
        {
            public string? Test { get; set; }
        }
    }
}
