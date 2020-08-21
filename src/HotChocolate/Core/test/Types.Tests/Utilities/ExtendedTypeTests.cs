#nullable enable

using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Utilities
{
    public class ExtendedTypeTests
    {
        [Fact]
        public void From_SystemType_Array()
        {
            // arrange
            // act
            ExtendedType extendedType = ExtendedType.FromType(typeof(byte[]));

            // assert
            Assert.True(extendedType.IsArray);
            Assert.Collection(extendedType.TypeArguments.Select(t => t.Type),
                t => Assert.Equal(typeof(byte), t));
        }

        [Fact]
        public void From_SystemType_List()
        {
            // arrange
            // act
            ExtendedType list = ExtendedType.FromType(
                typeof(NonNullType<NativeType<List<byte?>>>));

            ExtendedType nullableList = ExtendedType.FromType(
                typeof(List<byte?>));

            // assert
            Assert.True(list.IsList);
            Assert.True(list.IsCollection);
            Assert.False(list.IsNullable);
            Assert.True(nullableList.IsList);
            Assert.True(nullableList.IsCollection);
            Assert.True(nullableList.IsNullable);
        }

        [Fact]
        public void From_SystemType_Dict()
        {
            // arrange
            // act
            ExtendedType dict = ExtendedType.FromType(typeof(Dictionary<string, string>));

            // assert
            Assert.True(dict.IsList);
            Assert.True(dict.IsCollection);
        }

        [Fact]
        public void From_SchemaType_ListOfString()
        {
            // arrange
            // act
            ExtendedType extendedType = ExtendedType.FromType(typeof(ListType<StringType>));

            // assert
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
                typeof(NonNullType<ListType<StringType>>));

            // assert
            Assert.True(extendedType.IsGeneric);
            Assert.False(extendedType.IsNullable);
        }

        [Fact]
        public void IsEqual_Byte_Byte_True()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte));
            ExtendedType b = ExtendedType.FromType(typeof(byte));

            // act
            bool result = a.Equals(b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEqual_Object_Byte_Byte_True()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte));
            ExtendedType b = ExtendedType.FromType(typeof(byte));

            // act
            bool result = a.Equals((object)b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEqual_Ref_Byte_Byte_True()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte));

            // act
            bool result = a.Equals(a);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEqual_Ref_Object_Byte_Byte_True()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte));

            // act
            bool result = a.Equals((object)a);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEqual_Byte_Null_False()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte));

            // act
            bool result = a.Equals(default(ExtendedType));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsEqual_Object_Byte_Null_False()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte));

            // act
            bool result = a.Equals(default(object));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsEqual_Byte_String_False()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte));
            ExtendedType b = ExtendedType.FromType(typeof(string));

            // act
            bool result = a.Equals(b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsEqual_Object_Byte_String_False()
        {
            // arrange
            ExtendedType a = ExtendedType.FromType(typeof(byte));
            ExtendedType b = ExtendedType.FromType(typeof(string));

            // act
            bool result = a.Equals((object)b);

            // assert
            Assert.False(result);
        }
    }
}
