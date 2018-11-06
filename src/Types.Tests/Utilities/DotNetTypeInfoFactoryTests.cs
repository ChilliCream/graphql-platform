using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Utilities
{
    public class DotNetTypeInfoFactoryTests
    {
        [InlineData(typeof(string), "String")]
        [InlineData(typeof(IResolverResult<string>), "String")]
        [InlineData(typeof(IResolverResult<string[]>), "[String]")]
        [InlineData(typeof(IResolverResult<List<string>>), "[String]")]
        [InlineData(typeof(Task<IResolverResult<string>>), "String")]
        [InlineData(typeof(Task<IResolverResult<string[]>>), "[String]")]
        [InlineData(typeof(Task<IResolverResult<List<string>>>), "[String]")]
        [InlineData(typeof(ResolverResult<string>), "String")]
        [InlineData(typeof(ResolverResult<string[]>), "[String]")]
        [InlineData(typeof(ResolverResult<List<string>>), "[String]")]
        [InlineData(typeof(Task<string>), "String")]
        [InlineData(typeof(List<string>), "[String]")]
        [InlineData(typeof(Task<List<string>>), "[String]")]
        [InlineData(typeof(string[]), "[String]")]
        [InlineData(typeof(Task<string[]>), "[String]")]
        [InlineData(typeof(NativeType<string>), "String")]
        [InlineData(typeof(NativeType<Task<string>>), "String")]
        [InlineData(typeof(NativeType<List<string>>), "[String]")]
        [InlineData(typeof(NativeType<Task<List<string>>>), "[String]")]
        [InlineData(typeof(NativeType<string[]>), "[String]")]
        [InlineData(typeof(NativeType<Task<string[]>>), "[String]")]
        [Theory]
        public void CreateTypeInfoFromReferenceType(
            Type nativeType,
            string expectedTypeName)
        {
            // arrange
            DotNetTypeInfoFactory factory = new DotNetTypeInfoFactory();

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);

            // assert
            Assert.True(success);
            Assert.Equal(expectedTypeName,
                typeInfo.TypeFactory(new StringType()).Visualize());
        }

        [InlineData(typeof(int), "Int!")]
        [InlineData(typeof(Task<int>), "Int!")]
        [InlineData(typeof(List<int>), "[Int!]")]
        [InlineData(typeof(Task<List<int>>), "[Int!]")]
        [InlineData(typeof(int[]), "[Int!]")]
        [InlineData(typeof(Task<int[]>), "[Int!]")]
        [InlineData(typeof(NativeType<int>), "Int!")]
        [InlineData(typeof(NativeType<Task<int>>), "Int!")]
        [InlineData(typeof(NativeType<List<int>>), "[Int!]")]
        [InlineData(typeof(NativeType<Task<List<int>>>), "[Int!]")]
        [InlineData(typeof(NativeType<int[]>), "[Int!]")]
        [InlineData(typeof(NativeType<Task<int[]>>), "[Int!]")]
        [InlineData(typeof(int?), "Int")]
        [InlineData(typeof(Task<int?>), "Int")]
        [InlineData(typeof(List<int?>), "[Int]")]
        [InlineData(typeof(Task<List<int?>>), "[Int]")]
        [InlineData(typeof(int?[]), "[Int]")]
        [InlineData(typeof(Task<int?[]>), "[Int]")]
        [InlineData(typeof(NativeType<int?>), "Int")]
        [InlineData(typeof(NativeType<Task<int?>>), "Int")]
        [InlineData(typeof(NativeType<List<int?>>), "[Int]")]
        [InlineData(typeof(NativeType<Task<List<int?>>>), "[Int]")]
        [InlineData(typeof(NativeType<int?[]>), "[Int]")]
        [InlineData(typeof(NativeType<Task<int?[]>>), "[Int]")]
        [Theory]
        public void CreateTypeInfoFromValueType(Type nativeType, string expectedTypeName)
        {
            // arrange
            DotNetTypeInfoFactory factory = new DotNetTypeInfoFactory();

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);

            // assert
            Assert.True(success);
            Assert.Equal(expectedTypeName,
                typeInfo.TypeFactory(new IntType()).Visualize());
        }

        [InlineData(typeof(NativeType<StringType>))]
        [InlineData(typeof(Task<List<Task<StringType>>>))]
        [InlineData(typeof(NonNullType<ListType<NonNullType<StringType>>>))]
        [InlineData(typeof(Task<List<Task<int>>>))]
        [InlineData(typeof(List<Task<int>>))]
        [InlineData(typeof(NativeType<NativeType<Task<int?[]>>>))]
        [Theory]
        public void NotSupportedCases(Type nativeType)
        {
            // arrange
            DotNetTypeInfoFactory factory = new DotNetTypeInfoFactory();

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);

            // assert
            Assert.False(success);
        }

        [InlineData(typeof(CustomStringList), "[String]")]
        [InlineData(typeof(List<string>), "[String]")]
        [InlineData(typeof(Collection<string>), "[String]")]
        [InlineData(typeof(ReadOnlyCollection<string>), "[String]")]
        [InlineData(typeof(ImmutableList<string>), "[String]")]
        [InlineData(typeof(ImmutableArray<string>), "[String]")]
        [InlineData(typeof(IList<string>), "[String]")]
        [InlineData(typeof(ICollection<string>), "[String]")]
        [InlineData(typeof(IEnumerable<string>), "[String]")]
        [InlineData(typeof(IReadOnlyCollection<string>), "[String]")]
        [InlineData(typeof(IReadOnlyList<string>), "[String]")]
        [InlineData(typeof(string[]), "[String]")]
        [Theory]
        public void SupportedListTypes(Type nativeType, string expectedTypeName)
        {
            // arrange
            DotNetTypeInfoFactory factory = new DotNetTypeInfoFactory();

            // act
            bool success = factory.TryCreate(nativeType, out TypeInfo typeInfo);

            // assert
            Assert.True(success);
            Assert.Equal(expectedTypeName,
                typeInfo.TypeFactory(new StringType()).Visualize());
        }

        private class CustomStringList
            : CustomStringListBase
        {
        }

        private class CustomStringListBase
            : List<string>
        {
        }
    }
}
