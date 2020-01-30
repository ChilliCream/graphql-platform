using System;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Utilities
{
    public class NamedTypeInfoFactoryTests
    {
        [Fact]
        public void Case4()
        {
            // arrange
            var factory = new NamedTypeInfoFactory();
            Type nativeType =
                typeof(NonNullType<ListType<NonNullType<StringType>>>);

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);
            IType type = typeInfo.TypeFactory(new StringType());

            // assert
            Assert.True(success);
            Assert.IsType<NonNullType>(type);
            type = ((NonNullType)type).Type as IOutputType;
            Assert.IsType<ListType>(type);
            type = ((ListType)type).ElementType as IOutputType;
            Assert.IsType<NonNullType>(type);
            type = ((NonNullType)type).Type as IOutputType;
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void Case3_1()
        {
            // arrange
            var factory = new NamedTypeInfoFactory();
            Type nativeType = typeof(ListType<NonNullType<StringType>>);

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);
            IType type = typeInfo.TypeFactory(new StringType());

            // assert
            Assert.True(success);
            Assert.IsType<ListType>(type);
            type = ((ListType)type).ElementType as IOutputType;
            Assert.IsType<NonNullType>(type);
            type = ((NonNullType)type).Type as IOutputType;
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void Case3_2()
        {
            // arrange
            var factory = new NamedTypeInfoFactory();
            Type nativeType = typeof(NonNullType<ListType<StringType>>);

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);
            IType type = typeInfo.TypeFactory(new StringType());

            // assert
            Assert.True(success);
            Assert.IsType<NonNullType>(type);
            type = ((NonNullType)type).Type as IOutputType;
            Assert.IsType<ListType>(type);
            type = ((ListType)type).ElementType as IOutputType;
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void Case2_1()
        {
            // arrange
            var factory = new NamedTypeInfoFactory();
            Type nativeType = typeof(NonNullType<StringType>);

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);
            IType type = typeInfo.TypeFactory(new StringType());

            // assert
            Assert.True(success);
            Assert.IsType<NonNullType>(type);
            type = ((NonNullType)type).Type as IOutputType;
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void Case2_2()
        {
            // arrange
            var factory = new NamedTypeInfoFactory();
            Type nativeType = typeof(ListType<StringType>);

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);
            IType type = typeInfo.TypeFactory(new StringType());

            // assert
            Assert.True(success);
            Assert.IsType<ListType>(type);
            type = ((ListType)type).ElementType as IOutputType;
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void Case1()
        {
            // arrange
            var factory = new NamedTypeInfoFactory();
            Type nativeType = typeof(StringType);

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);
            IType type = typeInfo.TypeFactory(new StringType());

            // assert
            Assert.True(success);
            Assert.IsType<StringType>(type);
        }

        [InlineData(typeof(string))]
        [InlineData(typeof(NativeType<StringType>))]
        [InlineData(typeof(NativeType<string>))]
        [Theory]
        public void NotSupportedCases(Type nativeType)
        {
            // arrange
            var factory = new NamedTypeInfoFactory();

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);

            // assert
            Assert.False(success);
        }
    }
}
