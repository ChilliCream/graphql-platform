﻿using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class ShortTypeTests
    {
        [Fact]
        public void IsInstanceOfType_FloatLiteral_True()
        {
            // arrange
            var type = new ShortType();
            var literal = new IntValueNode(1);

            // act
            var result = type.IsInstanceOfType(literal);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_NullLiteral_True()
        {
            // arrange
            var type = new ShortType();

            // act
            var result = type.IsInstanceOfType(NullValueNode.Default);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_StringLiteral_False()
        {
            // arrange
            var type = new ShortType();

            // act
            var result = type.IsInstanceOfType(new FloatValueNode(1M));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_Null_Throws()
        {
            // arrange
            var type = new ShortType();

            // act
            // assert
            Assert.Throws<ArgumentNullException>(
                () => type.IsInstanceOfType(null));
        }

        [Fact]
        public void Serialize_Type()
        {
            // arrange
            var type = new ShortType();
            short value = 123;

            // act
            var serializedValue = type.Serialize(value);

            // assert
            Assert.IsType<short>(serializedValue);
            Assert.Equal(value, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var type = new ShortType();

            // act
            var serializedValue = type.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_Wrong_Type_Throws()
        {
            // arrange
            var type = new ShortType();
            var input = "abc";

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.Serialize(input));
        }

        [Fact]
        public void Serialize_MaxValue_Violation()
        {
            // arrange
            var type = new ShortType(0, 100);
            short value = 200;

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.Serialize(value));
        }

        [Fact]
        public void ParseLiteral_IntLiteral()
        {
            // arrange
            var type = new ShortType();
            var literal = new IntValueNode(1);

            // act
            var value = type.ParseLiteral(literal);

            // assert
            Assert.IsType<short>(value);
            Assert.Equal(literal.ToInt16(), value);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var type = new ShortType();

            // act
            var output = type.ParseLiteral(NullValueNode.Default);

            // assert
            Assert.Null(output);
        }

        [Fact]
        public void ParseLiteral_Wrong_ValueNode_Throws()
        {
            // arrange
            var type = new ShortType();
            var input = new StringValueNode("abc");

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.ParseLiteral(input));
        }

        [Fact]
        public void ParseLiteral_Null_Throws()
        {
            // arrange
            var type = new ShortType();

            // act
            // assert
            Assert.Throws<ArgumentNullException>(
                () => type.ParseLiteral(null));
        }

        [Fact]
        public void ParseValue_MaxValue()
        {
            // arrange
            var type = new ShortType(1, 100);
            short input = 100;

            // act
            var literal = (IntValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(100, literal.ToByte());
        }

        [Fact]
        public void ParseValue_MaxValue_Violation()
        {
            // arrange
            var type = new ShortType(1, 100);
            short input = 101;

            // act
            Action action = () => type.ParseValue(input);

            // assert
            Assert.Throws<ScalarSerializationException>(action);
        }

        [Fact]
        public void ParseValue_MinValue()
        {
            // arrange
            var type = new ShortType(1, 100);
            short input = 1;

            // act
            var literal = (IntValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(1, literal.ToByte());
        }

        [Fact]
        public void ParseValue_MinValue_Violation()
        {
            // arrange
            var type = new ShortType(1, 100);
            short input = 0;

            // act
            Action action = () => type.ParseValue(input);

            // assert
            Assert.Throws<ScalarSerializationException>(action);
        }


        [Fact]
        public void ParseValue_Wrong_Value_Throws()
        {
            // arrange
            var type = new ShortType();
            var value = "123";

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.ParseValue(value));
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            var type = new ShortType();
            object input = null;

            // act
            object output = type.ParseValue(input);

            // assert
            Assert.IsType<NullValueNode>(output);
        }

        [Fact]
        public void ParseValue_Nullable()
        {
            // arrange
            var type = new ShortType();
            short? input = 123;

            // act
            IntValueNode output = (IntValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(123, output.ToDouble());
        }

        [Fact]
        public void Ensure_TypeKind_is_Scalar()
        {
            // arrange
            var type = new ShortType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, kind);
        }
    }
}
