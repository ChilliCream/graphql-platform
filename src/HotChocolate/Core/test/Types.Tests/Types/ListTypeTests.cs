using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using Moq;
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
        public void IsInstanceOfType_OutputType_InvalidOperationException()
        {
            // arrange
            ObjectType innerType = Mock.Of<ObjectType>();
            var listType = (IInputType)new ListType(innerType);
            IValueNode literal = Mock.Of<IValueNode>();

            // act
            Action action = () => listType.IsInstanceOfType(literal);

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
            Type clrType = type.RuntimeType;

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
        public void IsInstanceOfType_ListOfString()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new List<string> { "foo" };

            // act
            bool isInstanceOf = listType.IsInstanceOfType(list);

            // assert
            Assert.True(isInstanceOf);
        }

        [Fact]
        public void IsInstanceOfType_StringArray()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new string[] { "foo" };

            // act
            bool isInstanceOf = listType.IsInstanceOfType(list);

            // assert
            Assert.True(isInstanceOf);
        }

        [Fact]
        public void IsInstanceOfType_Enumerable_NoElementType()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new List<object> { "foo" }.Select(t => t);

            // act
            bool isInstanceOf = listType.IsInstanceOfType(list);

            // assert
            Assert.False(isInstanceOf);
        }

        [Fact]
        public void IsInstanceOfType_Null()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());

            // act
            bool isInstanceOf = listType.IsInstanceOfType((object)null);

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
        public void IsInstanceOfType_Value_OutputType_InvalidOperationExcept()
        {
            // arrange
            ObjectType innerType = Mock.Of<ObjectType>();
            var listType = (IInputType)new ListType(innerType);
            var list = new List<object> { };

            // act
            Action action = () => listType.IsInstanceOfType(list);

            // assert
            Assert.Throws<InvalidOperationException>(action);
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
        public void Serialize_Null()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());

            // act
            var serializedList = listType.Serialize(null);

            // assert
            Assert.Null(serializedList);
        }

        [Fact]
        public void Serialize_OutputType_InvalidOperationException()
        {
            // arrange
            ObjectType innerType = Mock.Of<ObjectType>();
            var listType = (IInputType)new ListType(innerType);
            var list = new string[] { "abc" };

            // act
            Action action = () => listType.Serialize(list);

            // assert
            Assert.Throws<InvalidOperationException>(action);
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

        [Fact]
        public void TryDeserialize_ListObject_To_ListString()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new List<object> { "abc" };

            // act
            var success = listType.TryDeserialize(list, out var serializedList);

            // assert
            Assert.True(success);
            Assert.False(object.ReferenceEquals(list, serializedList));
            Assert.Collection(
                Assert.IsType<List<string>>(serializedList),
                t => Assert.Equal("abc", t));
        }

        [Fact]
        public void Deserialize_Null()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());

            // act
            var deserialized = listType.Deserialize(null);

            // assert
            Assert.Null(deserialized);
        }

        [Fact]
        public void TryDeserialize_MixedList_CannotDeserialize()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new List<object> { "abc", 123 };

            // act
            var success = listType.TryDeserialize(list, out var serializedList);

            // assert
            Assert.False(success);
            Assert.Null(serializedList);
        }

        [Fact]
        public void Deserialize_OutputType_InvalidOperationException()
        {
            // arrange
            ObjectType innerType = Mock.Of<ObjectType>();
            var listType = (IInputType)new ListType(innerType);
            var list = new List<object> { "abc" };

            // act
            Action action = () => listType.Deserialize(list);

            // assert
            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void TryDeserialize_OutputType_InvalidOperationException()
        {
            // arrange
            ObjectType innerType = Mock.Of<ObjectType>();
            var listType = (IInputType)new ListType(innerType);
            var list = new List<object> { "abc" };

            // act
            Action action = () => listType.TryDeserialize(list, out _);

            // assert
            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void ParseLiteral_ListValue_To_ListString()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var list = new ListValueNode(new StringValueNode("abc"));

            // act
            var serializedList = listType.ParseLiteral(list);

            // assert
            Assert.Collection(
                Assert.IsType<List<string>>(serializedList),
                t => Assert.Equal("abc", t));
        }

        [Fact]
        public void ParseLiteral_StringLiteral_To_ListString()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var literal = new StringValueNode("abc");

            // act
            var serializedList = listType.ParseLiteral(literal);

            // assert
            Assert.Collection(
                Assert.IsType<List<string>>(serializedList),
                t => Assert.Equal("abc", t));
        }

        [Fact]
        public void ParseLiteral_OutputType_InvalidOperationException()
        {
            // arrange
            ObjectType innerType = Mock.Of<ObjectType>();
            var listType = (IInputType)new ListType(innerType);
            var literal = new StringValueNode("abc");

            // act
            Action action = () => listType.ParseLiteral(literal);

            // assert
            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void ParseLiteral_LiteralIsNull_ArgumentNullException()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());

            // act
            Action action = () => listType.ParseLiteral(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void ParseValue_StringList_To_ListValue()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());
            var abc = new List<string> { "abc" };

            // act
            IValueNode literal = listType.ParseValue(abc);

            // assert
            Assert.Collection(
                Assert.IsType<ListValueNode>(literal).Items,
                t => Assert.Equal(
                    "abc",
                    Assert.IsType<StringValueNode>(t).Value));
        }


        [Fact]
        public void ParseValue_Null_To_NullValue()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());

            // act
            IValueNode literal = listType.ParseValue(null);

            // assert
            Assert.IsType<NullValueNode>(literal);
        }

        [Fact]
        public void ParseValue_OutputType_InvalidOperationException()
        {
            // arrange
            ObjectType innerType = Mock.Of<ObjectType>();
            var listType = (IInputType)new ListType(innerType);
            var value = new List<string>();

            // act
            Action action = () => listType.ParseValue(value);

            // assert
            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void ParseValue_Int_CannotParse()
        {
            // arrange
            var listType = (IInputType)new ListType(new StringType());

            // act
            Action action = () => listType.ParseValue(1);

            // assert
            Assert.Throws<SerializationException>(action);
        }
    }
}
