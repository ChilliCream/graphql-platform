using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class NativeTypeTests
    {
        [Fact]
        public void Kind_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            TypeKind kind;
            Action a = () => kind = type.Kind;

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        [Fact]
        public void ClrType_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            Type clrType;
            Action a = () => clrType = type.ClrType;

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        [Fact]
        public void IsInstanceOfType_ValueNode_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            Action a = () => type.IsInstanceOfType(default(IValueNode));

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        [Fact]
        public void IsInstanceOfType_Object_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            Action a = () => type.IsInstanceOfType(default(object));

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        [Fact]
        public void ParseLiteral_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            Action a = () => type.ParseLiteral(default(IValueNode));

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        [Fact]
        public void ParseValue_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            Action a = () => type.ParseValue(default(object));

            // assert
            Assert.Throws<NotSupportedException>(a);
        }
    }
}
