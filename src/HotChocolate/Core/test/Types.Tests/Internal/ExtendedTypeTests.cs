using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Types;
using Xunit;

#nullable enable

namespace HotChocolate.Internal
{
    public class ExtendedTypeTests
    {
        private readonly TypeCache _cache = new TypeCache();

        [Fact]
        public void From_SystemType_Array()
        {
            // arrange
            // act
            ExtendedType extendedType = ExtendedType.FromType(typeof(byte[]), _cache);

            // assert
            Assert.True(extendedType.IsArray);
            Assert.Collection(
                extendedType.TypeArguments.Select(t => t.Type),
                t => Assert.Equal(typeof(byte), t));
        }

        [Fact]
        public void From_SystemType_List()
        {
            // arrange
            // act
            IExtendedType list = ExtendedType.FromType(
                typeof(NativeType<List<byte?>>),
                _cache);
            list = ExtendedType.Tools.ChangeNullability(
                list, new bool?[] { false }, _cache);

            ExtendedType nullableList = ExtendedType.FromType(
                typeof(List<byte?>),
                _cache);

            // assert
            Assert.True(list.IsList);
            Assert.True(list.IsArrayOrList);
            Assert.False(list.IsNullable);
            Assert.True(nullableList.IsList);
            Assert.True(nullableList.IsArrayOrList);
            Assert.True(nullableList.IsNullable);
        }

        [Fact]
        public void From_SystemType_Dict()
        {
            // arrange
            // act
            ExtendedType dict = ExtendedType.FromType(
                typeof(Dictionary<string, string>),
                _cache);

            // assert
            Assert.True(dict.IsList);
            Assert.True(dict.IsArrayOrList);
        }

        [Fact]
        public void From_SchemaType_ListOfString()
        {
            // arrange
            // act
            ExtendedType extendedType = ExtendedType.FromType(typeof(ListType<StringType>), _cache);

            // assert
            Assert.True(extendedType.IsSchemaType);
            Assert.True(extendedType.IsGeneric);
            Assert.Collection(extendedType.TypeArguments.Select(t => t.Type),
                t => Assert.Equal(typeof(StringType), t));
        }

        [Fact]
        public void From_SchemaType_NonNullListOfString()
        {
            // arrange
            // act
            ExtendedType extendedType = ExtendedType.FromType(
                typeof(NonNullType<ListType<StringType>>),
                _cache);

            // assert
            Assert.True(extendedType.IsSchemaType);
            Assert.True(extendedType.IsGeneric);
            Assert.False(extendedType.IsNullable);
        }

        [Fact]
        public void From_IntType()
        {
            // arrange
            // act
            ExtendedType extendedType = ExtendedType.FromType(
                typeof(IntType),
                _cache);

            // assert
            Assert.True(extendedType.IsSchemaType);
            Assert.False(extendedType.IsGeneric);
            Assert.True(extendedType.IsNullable);
        }

        [Fact]
        public void From_InputObjectOfIntType()
        {
            // arrange
            // act
            ExtendedType extendedType = ExtendedType.FromType(
                typeof(InputObjectType<IntType>),
                _cache);

            // assert
            Assert.True(extendedType.IsSchemaType);
            Assert.True(extendedType.IsGeneric);
            Assert.True(extendedType.IsNamedType);
            Assert.True(extendedType.IsNullable);

            IExtendedType argument = extendedType.TypeArguments[0];
            Assert.True(argument.IsSchemaType);
            Assert.False(argument.IsGeneric);
            Assert.True(extendedType.IsNamedType);
            Assert.True(argument.IsNullable);
        }

        [Fact]
        public void From_NativeTypeIntType()
        {
            // arrange
            // act
            ExtendedType extendedType = ExtendedType.FromType(
                typeof(NativeType<IntType>),
                _cache);

            // assert
            Assert.True(extendedType.IsSchemaType);
            Assert.False(extendedType.IsGeneric);
            Assert.True(extendedType.IsNullable);
        }

        [Fact]
        public void Schema_Type_Cache_Id_Distinguishes_Between_NonNull_And_Nullable()
        {
            // arrange
            // act
            ExtendedType extendedType1 = ExtendedType.FromType(
                typeof(NonNullType<ListType<StringType>>),
                _cache);

            ExtendedType extendedType2 = ExtendedType.FromType(
                typeof(NonNullType<StringType>),
                _cache);

            ExtendedType extendedType3 = ExtendedType.FromType(
                typeof(ListType<StringType>),
                _cache);

            ExtendedType extendedType4 = ExtendedType.FromType(
                typeof(StringType),
                _cache);

            // assert
            Assert.False(extendedType1.IsNullable);
            Assert.False(extendedType2.IsNullable);
            Assert.True(extendedType3.IsNullable);
            Assert.True(extendedType4.IsNullable);
        }

        [Fact]
        public void IsEqual_Byte_Byte_True()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte), _cache);
            ExtendedType b = ExtendedType.FromType(typeof(byte), _cache);

            // act
            var result = a.Equals(b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEqual_Object_Byte_Byte_True()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte), _cache);
            ExtendedType b = ExtendedType.FromType(typeof(byte), _cache);

            // act
            var result = a.Equals((object)b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEqual_Ref_Byte_Byte_True()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte), _cache);

            // act
            var result = a.Equals(a);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEqual_Ref_Object_Byte_Byte_True()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte), _cache);

            // act
            var result = a.Equals((object)a);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEqual_Byte_Null_False()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte), _cache);

            // act
            var result = a.Equals(default(ExtendedType));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsEqual_Object_Byte_Null_False()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte), _cache);

            // act
            var result = a.Equals(default(object));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsEqual_Byte_String_False()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte), _cache);
            ExtendedType b = ExtendedType.FromType(typeof(string), _cache);

            // act
            var result = a.Equals(b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsEqual_Object_Byte_String_False()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte), _cache);
            ExtendedType b = ExtendedType.FromType(typeof(string), _cache);

            // act
            var result = a.Equals((object)b);

            // assert
            Assert.False(result);
        }

        [InlineData(typeof(CustomStringList1), "CustomStringList1", "String")]
        [InlineData(typeof(CustomStringList2<string>), "CustomStringList2<String>", "String")]
        [InlineData(
            typeof(CustomStringList3<string, int>),
            "CustomStringList3<String, Int32!>",
            "String")]
        [InlineData(typeof(List<string>), "List<String>", "String")]
        [InlineData(typeof(Collection<string>), "Collection<String>", "String")]
        [InlineData(typeof(ReadOnlyCollection<string>), "ReadOnlyCollection<String>", "String")]
        [InlineData(typeof(ImmutableList<string>), "ImmutableList<String>", "String")]
        [InlineData(typeof(ImmutableArray<string>), "ImmutableArray<String>!", "String")]
        [InlineData(typeof(IList<string>), "IList<String>", "String")]
        [InlineData(typeof(ICollection<string>), "ICollection<String>", "String")]
        [InlineData(typeof(IEnumerable<string>), "IEnumerable<String>", "String")]
        [InlineData(typeof(IReadOnlyCollection<string>), "IReadOnlyCollection<String>", "String")]
        [InlineData(typeof(IReadOnlyList<string>), "IReadOnlyList<String>", "String")]
        [InlineData(typeof(IExecutable<string>), "IExecutable<String>", "String")]
        [InlineData(typeof(string[]), "[String]", "String")]
        [InlineData(
            typeof(Task<IAsyncEnumerable<string>>),
            "IAsyncEnumerable<String>",
            "String")]
        [InlineData(
            typeof(ValueTask<IAsyncEnumerable<string>>),
            "IAsyncEnumerable<String>",
            "String")]
        [Theory]
        public void SupportedListTypes(Type type, string listTypeName, string elementTypeName)
        {
            // arrange
            // act
            ExtendedType extendedType = ExtendedType.FromType(type, _cache);

            // assert
            Assert.Equal(listTypeName, extendedType.ToString());
            Assert.Equal(elementTypeName, extendedType.ElementType?.ToString());
        }

        [InlineData(typeof(List<string>))]
        [InlineData(typeof(IReadOnlyList<string>))]
        [InlineData(typeof(IList<string>))]
        [InlineData(typeof(CustomStringList2<string>))]
        [InlineData(typeof(ImmutableArray<string>))]
        [InlineData(typeof(IEnumerable<string>))]
        [InlineData(typeof(IExecutable<string>))]
        [InlineData(typeof(Task<IAsyncEnumerable<string>>))]
        [InlineData(typeof(ValueTask<IAsyncEnumerable<string>>))]
        [Theory]
        public void ChangeNullability_From_ElementType(Type listType)
        {
            // arrange
            // act
            IExtendedType list = ExtendedType.FromType(listType, _cache);
            list = ExtendedType.Tools.ChangeNullability(
                list, new bool?[] { null, false }, _cache);

            // assert
            Assert.False(list.ElementType!.IsNullable);
            Assert.Same(list.ElementType, list.TypeArguments[0]);
        }

        [Fact]
        public void NullableOptionalNullableString()
        {
            // arrange
            MethodInfo member =
                typeof(Nullability).GetMethod(nameof(Nullability.NullableOptionalNullableString))!;

            // act
            ExtendedType type = ExtendedType.FromMember(member, _cache);

            // assert
            Assert.Equal("Optional<String>", type.ToString());
        }

        [Fact]
        public void OptionalNullableOptionalNullableString()
        {
            // arrange
            MethodInfo member =
                typeof(Nullability)
                    .GetMethod(nameof(Nullability.OptionalNullableOptionalNullableString))!;

            // act
            ExtendedType type = ExtendedType.FromMember(member, _cache);

            // assert
            Assert.Equal("Optional<Optional<String>>!", type.ToString());
        }

        [Fact]
        public void From_IExecutableScalar()
        {
            // arrange
            // act
            ExtendedType dict = ExtendedType.FromType(
                typeof(IExecutable<string>),
                _cache);

            // assert
            Assert.True(dict.IsList);
            Assert.True(dict.IsArrayOrList);
        }

        private class CustomStringList1
            : List<string>
        {
        }

        private class CustomStringList2<T>
            : List<T>
            where T : notnull
        {
        }

        private class CustomStringList3<T, TK>
            : List<T>
            where T : notnull
        {
            public TK Foo { get; set; }
        }

#nullable enable

        public class Nullability
        {
            public Nullable<Optional<string?>> NullableOptionalNullableString() =>
                throw new NotImplementedException();

            public Optional<Nullable<Optional<string?>>> OptionalNullableOptionalNullableString() =>
                throw new NotImplementedException();
        }

#nullable disable
    }
}
