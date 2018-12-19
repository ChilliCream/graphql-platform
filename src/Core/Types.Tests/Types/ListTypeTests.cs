using System;
using System.Collections.Generic;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class ListTypeTests
    {
        [Fact]
        public void EnsureElementTypeIsCorrectlySet()
        {
            // arrange
            StringType innerType = new StringType();

            // act
            ListType type = new ListType(innerType);

            // assert
            Assert.Equal(innerType, type.ElementType);
        }


        [Fact]
        public void EnsureNonNullElementTypeIsCorrectlySet()
        {
            // arrange
            NonNullType innerType = new NonNullType(new StringType());

            // act
            ListType type = new ListType(innerType);

            // assert
            Assert.Equal(innerType, type.ElementType);
        }

        [Fact]
        public void EnsureNativeTypeIsCorrectlyDetected()
        {
            // arrange
            NonNullType innerType = new NonNullType(new StringType());
            ListType type = new ListType(innerType);

            // act
            Type clrType = type.ClrType;

            // assert
            Assert.Equal(typeof(List<string>), clrType);
        }

        [Fact]
        public void EnsureInstanceOfIsDelegatedToInnerType()
        {
            // arrange
            NonNullType innerType = new NonNullType(new StringType());

            // act
            ListType type = new ListType(innerType);
            bool shouldBeFalse = type.IsInstanceOfType(
                new ListValueNode(new[] { NullValueNode.Default }));
            bool shouldBeTrue = type.IsInstanceOfType(
                new ListValueNode(new[] { new StringValueNode("foo") }));

            // assert
            Assert.False(shouldBeFalse);
            Assert.True(shouldBeTrue);
        }
    }
}
