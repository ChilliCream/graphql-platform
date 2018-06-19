using System;
using System.Linq;
using System.Threading;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Factories;
using Xunit;

namespace HotChocolate.Types
{
    public class NamedTypeInfoFactoryTests
    {
        [Fact]
        public void Case4()
        {
            // arrange
            NamedTypeInfoFactory factory = new NamedTypeInfoFactory();
            Type nativeType = typeof(NonNullType<ListType<NonNullType<StringType>>>);

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
            NamedTypeInfoFactory factory = new NamedTypeInfoFactory();
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
            NamedTypeInfoFactory factory = new NamedTypeInfoFactory();
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
            NamedTypeInfoFactory factory = new NamedTypeInfoFactory();
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
            NamedTypeInfoFactory factory = new NamedTypeInfoFactory();
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
            NamedTypeInfoFactory factory = new NamedTypeInfoFactory();
            Type nativeType = typeof(StringType);

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);
            IType type = typeInfo.TypeFactory(new StringType());

            // assert
            Assert.True(success);
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void NotSupportedCases()
        {
            // arrange
            NamedTypeInfoFactory factory = new NamedTypeInfoFactory();
            Type nativeType = typeof(NativeType<StringType>);

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);
            IType type = typeInfo.TypeFactory(new StringType());

            // assert
            Assert.True(success);
            Assert.IsType<StringType>(type);
        }
    }
}
