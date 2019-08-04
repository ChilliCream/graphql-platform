using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class ListTypeTests
    {
        [Fact]
        public void Create_ElementTypeNull_ArgNullExec()
        {
            // arrange
            // act
            Action action = () => new ListType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_ElementTypeIsListType_ArgExec()
        {
            // arrange
            // act
            Action action = () => new ListType(new ListType(new StringType()));

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void InstanceOf_LiteralIsNull_ArgNullExec()
        {
            // arrange
            // act
            Action action = () => ((IInputType)new ListType(new StringType()))
                .IsInstanceOfType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void InstanceOf_ElementTypeIsOutType_InvalidExec()
        {
            // arrange
            // act
            Action action = () => ((IInputType)new ListType(
                new ObjectType(c => c.Name("foo"))))
                .IsInstanceOfType(new StringValueNode("foo"));

            // assert
            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void EnsureElementTypeIsCorrectlySet()
        {
            // arrange
            var innerType = new StringType();

            // act
            var type = new ListType(innerType);

            // assert
            Assert.Equal(innerType, type.ElementType);
        }


        [Fact]
        public void EnsureNonNullElementTypeIsCorrectlySet()
        {
            // arrange
            var innerType = new NonNullType(new StringType());

            // act
            var type = new ListType(innerType);

            // assert
            Assert.Equal(innerType, type.ElementType);
        }

        [Fact]
        public void EnsureNativeTypeIsCorrectlyDetected()
        {
            // arrange
            var innerType = new NonNullType(new StringType());
            var type = new ListType(innerType);

            // act
            Type clrType = type.ClrType;

            // assert
            Assert.Equal(typeof(List<string>), clrType);
        }

        [Fact]
        public void EnsureInstanceOfIsDelegatedToInnerType()
        {
            // arrange
            var innerType = (IInputType)new NonNullType(new StringType());

            // act
            var type = (IInputType)new ListType(innerType);
            bool shouldBeFalse = type.IsInstanceOfType(
                new ListValueNode(new[] { NullValueNode.Default }));
            bool shouldBeTrue = type.IsInstanceOfType(
                new ListValueNode(new[] { new StringValueNode("foo") }));

            // assert
            Assert.False(shouldBeFalse);
            Assert.True(shouldBeTrue);
        }

        [Fact]
        public void IsInstanceOfType_ListObject_StringElements()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new List<object> { "foo" };

            // act
            bool isInstanceOf = listType.IsInstanceOfType(list);

            // assert
            Assert.True(isInstanceOf);
        }

        [Fact]
        public void IsInstanceOfType_ListObject_IntElements()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new List<object> { 1 };

            // act
            bool isInstanceOf = listType.IsInstanceOfType(list);

            // assert
            Assert.False(isInstanceOf);
        }

        [Fact]
        public void IsInstanceOfType_ListObject_Empty()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new List<object> { };

            // act
            bool isInstanceOf = listType.IsInstanceOfType(list);

            // assert
            Assert.True(isInstanceOf);
        }

        [Fact]
        public void Serialize_ListObject_To_ListObject()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new List<object> { "abc" };

            // act
            var serializedList = listType.Serialize(list);

            // assert
            Assert.False(object.ReferenceEquals(list, serializedList));
            Assert.Collection(
                Assert.IsType<List<object>>(serializedList),
                t => Assert.Equal("abc", t));
        }

        [Fact]
        public void Serialize_ListString_To_ListObject()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new List<string> { "abc" };

            // act
            var serializedList = listType.Serialize(list);

            // assert
            Assert.False(object.ReferenceEquals(list, serializedList));
            Assert.Collection(
                Assert.IsType<List<object>>(serializedList),
                t => Assert.Equal("abc", t));
        }

        [Fact]
        public void Serialize_StringArray_To_ListObject()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new string[] { "abc" };

            // act
            var serializedList = listType.Serialize(list);

            // assert
            Assert.False(object.ReferenceEquals(list, serializedList));
            Assert.Collection(
                Assert.IsType<List<object>>(serializedList),
                t => Assert.Equal("abc", t));
        }

        [Fact]
        public void Deserialize_ListObject_To_ListString()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new List<object> { "abc" };

            // act
            var serializedList = listType.Deserialize(list);

            // assert
            Assert.False(object.ReferenceEquals(list, serializedList));
            Assert.Collection(
                Assert.IsType<List<string>>(serializedList),
                t => Assert.Equal("abc", t));
        }
    }
}
