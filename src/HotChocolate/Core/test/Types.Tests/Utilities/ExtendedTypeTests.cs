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
            Internal.ExtendedType extendedType = Internal.ExtendedType.FromType(typeof(byte[]));

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
            Internal.ExtendedType list = Internal.ExtendedType.FromType(
                typeof(NonNullType<NativeType<List<byte?>>>));

            Internal.ExtendedType nullableList = Internal.ExtendedType.FromType(
                typeof(List<byte?>));

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
            Internal.ExtendedType dict = Internal.ExtendedType.FromType(typeof(Dictionary<string, string>));

            // assert
            Assert.True(dict.IsList);
            Assert.True(dict.IsArrayOrList);
        }

        [Fact]
        public void From_SchemaType_ListOfString()
        {
            // arrange
            // act
            Internal.ExtendedType extendedType = Internal.ExtendedType.FromType(typeof(ListType<StringType>));

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
            Internal.ExtendedType extendedType = Internal.ExtendedType.FromType(
                typeof(NonNullType<ListType<StringType>>));

            // assert
            Assert.True(extendedType.IsGeneric);
            Assert.False(extendedType.IsNullable);
        }

        [Fact]
        public void IsEqual_Byte_Byte_True()
        {
            // arrange
            Internal.ExtendedType a = Internal.ExtendedType.FromType(typeof(byte));
            Internal.ExtendedType b = Internal.ExtendedType.FromType(typeof(byte));

            // act
            bool result = a.Equals(b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEqual_Object_Byte_Byte_True()
        {
            // arrange
            Internal.ExtendedType a = Internal.ExtendedType.FromType(typeof(byte));
            Internal.ExtendedType b = Internal.ExtendedType.FromType(typeof(byte));

            // act
            bool result = a.Equals((object)b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEqual_Ref_Byte_Byte_True()
        {
            // arrange
            Internal.ExtendedType a = Internal.ExtendedType.FromType(typeof(byte));

            // act
            bool result = a.Equals(a);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEqual_Ref_Object_Byte_Byte_True()
        {
            // arrange
            Internal.ExtendedType a = Internal.ExtendedType.FromType(typeof(byte));

            // act
            bool result = a.Equals((object)a);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEqual_Byte_Null_False()
        {
            // arrange
            Internal.ExtendedType a = Internal.ExtendedType.FromType(typeof(byte));

            // act
            bool result = a.Equals(default(Internal.ExtendedType));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsEqual_Object_Byte_Null_False()
        {
            // arrange
            Internal.ExtendedType a = Internal.ExtendedType.FromType(typeof(byte));

            // act
            bool result = a.Equals(default(object));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsEqual_Byte_String_False()
        {
            // arrange
            Internal.ExtendedType a = Internal.ExtendedType.FromType(typeof(byte));
            Internal.ExtendedType b = Internal.ExtendedType.FromType(typeof(string));

            // act
            bool result = a.Equals(b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsEqual_Object_Byte_String_False()
        {
            // arrange
            Internal.ExtendedType a = Internal.ExtendedType.FromType(typeof(byte));
            Internal.ExtendedType b = Internal.ExtendedType.FromType(typeof(string));

            // act
            bool result = a.Equals((object)b);

            // assert
            Assert.False(result);
        }
    }
}
