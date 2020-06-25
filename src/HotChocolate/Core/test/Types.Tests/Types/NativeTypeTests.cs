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
            Action a = () => kind = ((IInputType)type).Kind;

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
            Action a = () => clrType = ((IInputType)type).RuntimeType;

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        [Fact]
        public void IsInstanceOfType_ValueNode_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            Action a = () => ((IInputType)type)
                .IsInstanceOfType(default(IValueNode));

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        [Fact]
        public void IsInstanceOfType_Object_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            Action a = () => ((IInputType)type)
                .IsInstanceOfType(default(object));

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        [Fact]
        public void ParseLiteral_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            Action a = () => ((IInputType)type)
                .ParseLiteral(default(IValueNode));

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        [Fact]
        public void ParseValue_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            Action a = () => ((IInputType)type)
                .ParseValue(default(object));

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        [Fact]
        public void Serialize_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            Action a = () => ((IInputType)type)
                .Serialize(default(object));

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        [Fact]
        public void Deserialize_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            Action a = () => ((IInputType)type)
                .Deserialize(default(object));

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        [Fact]
        public void TryDeserialize_NotSupportedException()
        {
            // arrange
            var type = new NativeType<string>();

            // act
            Action a = () => ((IInputType)type)
                .TryDeserialize(default(object), out var o);

            // assert
            Assert.Throws<NotSupportedException>(a);
        }
    }
}
