using System;
using System.Collections.Generic;
using HotChocolate.Types;
using Xunit;

#nullable enable

namespace HotChocolate.Utilities
{
    public class ExtendedTypeRewriterTests
    {
        [InlineData("NullableArray", typeof(byte[]))]
        [InlineData("Array", typeof(NonNullType<NativeType<byte[]>>))]
        [InlineData("ArrayNullableElement", typeof(NonNullType<NativeType<byte?[]>>))]
        [InlineData("NullableArrayNullableElement", typeof(byte?[]))]
        [Theory]
        public void Rewrite_ValueType_Arrays(string propertyName, Type expectedRewrittenType)
        {
            // arrange
            IExtendedType extendedType = typeof(Arrays)
                .GetMethod(propertyName)
                .GetExtendedMethodTypeInfo()
                .ReturnType;

            // act
            Type rewritten = ExtendedTypeRewriter.Rewrite(extendedType);

            // assert
            Assert.Equal(expectedRewrittenType, rewritten);
        }

        public class Arrays
        {
            public byte[] Array()
            {
                throw new NotImplementedException();
            }

            public byte[]? NullableArray()
            {
                throw new NotImplementedException();
            }

            public byte?[] ArrayNullableElement()
            {
                throw new NotImplementedException();
            }

            public byte?[]? NullableArrayNullableElement()
            {
                throw new NotImplementedException();
            }
        }
    }
}
