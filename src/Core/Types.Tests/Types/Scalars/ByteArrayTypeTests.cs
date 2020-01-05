﻿using System.Text;
using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class ByteArrayTypeTests
    {
        [Fact]
        public void IsInstanceOfType_StringLiteral()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            var byteArray = Encoding.ASCII.GetBytes("value");

            // act
            bool isOfType = byteArrayType.IsInstanceOfType(byteArray);

            // assert
            Assert.True(isOfType);
        }

        [Fact]
        public void IsInstanceOfType_NullLiteral()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            var literal = new NullValueNode(null);

            // act
            bool isOfType = byteArrayType.IsInstanceOfType(literal);

            // assert
            Assert.True(isOfType);
        }

        [Fact]
        public void IsInstanceOfType_IntLiteral()
        {
            // arrange
            var byteArrayType = new ByteArrayType();

            var literal = new IntValueNode(123);

            // act
            bool isOfType = byteArrayType.IsInstanceOfType(literal);

            // assert
            Assert.False(isOfType);
        }

        [Fact]
        public void IsInstanceOfType_Null()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            var guid = Guid.NewGuid();

            // act
            Action action = () => byteArrayType.IsInstanceOfType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Serialize_Base64()
        {
            // arrange
            var byteArrayType = new ByteArrayType();

            var value = Encoding.ASCII.GetBytes("value");

            // act
            var serializedValue = byteArrayType.Serialize(value);

            // assert
            Assert.Equal(value, Assert.IsType<byte[]>(serializedValue));
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var byteArrayType = new ByteArrayType();

            // act
            var serializedValue = byteArrayType.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_Int()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            var value = 123;

            // act
            Action action = () => byteArrayType.Serialize(value);

            // assert
            Assert.Throws<ScalarSerializationException>(action);
        }

        [Fact]
        public void Deserialize_Null()
        {
            // arrange
            var byteArrayType = new ByteArrayType();

            // act
            var success = byteArrayType.TryDeserialize(null, out object o);

            // assert
            Assert.True(success);
            Assert.Null(o);
        }

        [Fact]
        public void Deserialize_String()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            byte[] bytes = Encoding.ASCII.GetBytes("value");

            // act
            var success = byteArrayType.TryDeserialize(
                bytes, out object o);

            // assert
            Assert.True(success);
            Assert.Equal(bytes, o);
        }

        [Fact]
        public void Deserialize_Guid()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            byte[] bytes = Encoding.ASCII.GetBytes("value");

            // act
            var success = byteArrayType.TryDeserialize(bytes, out object o);

            // assert
            Assert.True(success);
            Assert.Equal(bytes, o);
        }

        [Fact]
        public void Deserialize_Int()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            var value = 123;

            // act
            var success = byteArrayType.TryDeserialize(value, out _);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            byte[] expected = Encoding.ASCII.GetBytes("value");
            var literal = new StringValueNode(Convert.ToBase64String(expected));

            // act
            var actual = (byte[])byteArrayType
                .ParseLiteral(literal);

            // assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseLiteral_IntValueNode()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            var literal = new IntValueNode(123);

            // act
            Action action = () => byteArrayType.ParseLiteral(literal);

            // assert
            Assert.Throws<ScalarSerializationException>(action);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            NullValueNode literal = NullValueNode.Default;

            // act
            var value = byteArrayType.ParseLiteral(literal);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void ParseLiteral_Null()
        {
            // arrange
            var byteArrayType = new ByteArrayType();

            // act
            Action action = () => byteArrayType.ParseLiteral(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void ParseValue_Guid()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            byte[] expected = Encoding.ASCII.GetBytes("value");
            var expectedLiteralValue = Convert.ToBase64String(expected);

            // act
            var stringLiteral =
                (StringValueNode)byteArrayType.ParseValue(expected);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            Guid? guid = null;

            // act
            IValueNode stringLiteral =
                byteArrayType.ParseValue(guid);

            // assert
            Assert.True(stringLiteral is NullValueNode);
            Assert.Null(((NullValueNode)stringLiteral).Value);
        }

        [Fact]
        public void ParseValue_Int()
        {
            // arrange
            var byteArrayType = new ByteArrayType();
            int value = 123;

            // act
            Action action = () => byteArrayType.ParseValue(value);

            // assert
            Assert.Throws<ScalarSerializationException>(action);
        }

        [Fact]
        public void EnsureDateTypeKindIsCorret()
        {
            // arrange
            var type = new ByteArrayType();

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }
    }
}
