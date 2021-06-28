using System;
using HotChocolate.Language;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Types
{
    public class UuidTypeTests
    {
        [Fact]
        public void IsInstanceOfType_StringLiteral()
        {
            // arrange
            var uuidType = new UuidType();
            var guid = Guid.NewGuid();

            // act
            var isOfType = uuidType.IsInstanceOfType(guid);

            // assert
            Assert.True(isOfType);
        }

        [Fact]
        public void IsInstanceOfType_NullLiteral()
        {
            // arrange
            var uuidType = new UuidType();
            var literal = new NullValueNode(null);

            // act
            var isOfType = uuidType.IsInstanceOfType(literal);

            // assert
            Assert.True(isOfType);
        }

        [Fact]
        public void IsInstanceOfType_IntLiteral()
        {
            // arrange
            var uuidType = new UuidType();
            var literal = new IntValueNode(123);

            // act
            var isOfType = uuidType.IsInstanceOfType(literal);

            // assert
            Assert.False(isOfType);
        }

        [Fact]
        public void IsInstanceOfType_Null()
        {
            // arrange
            var uuidType = new UuidType();

            // act
            void Action() => uuidType.IsInstanceOfType(null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Serialize_Guid()
        {
            // arrange
            var uuidType = new UuidType();
            var guid = Guid.NewGuid();

            // act
            object? serializedValue = uuidType.Serialize(guid);

            // assert
            Assert.Equal(guid.ToString("N"), Assert.IsType<string>(serializedValue));
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var uuidType = new UuidType();

            // act
            object? serializedValue = uuidType.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_Int()
        {
            // arrange
            var uuidType = new UuidType();
            var value = 123;

            // act
            void Action() => uuidType.Serialize(value);

            // assert
            Assert.Throws<SerializationException>(Action);
        }

        [Fact]
        public void Deserialize_Null()
        {
            // arrange
            var uuidType = new UuidType();

            // act
            var success = uuidType.TryDeserialize(null, out object? o);

            // assert
            Assert.True(success);
            Assert.Null(o);
        }

        [Fact]
        public void Deserialize_String()
        {
            // arrange
            var uuidType = new UuidType();
            var guid = Guid.NewGuid();

            // act
            var success = uuidType.TryDeserialize(guid.ToString("N"), out object? o);

            // assert
            Assert.True(success);
            Assert.Equal(guid, o);
        }

        [Fact]
        public void Deserialize_Guid()
        {
            // arrange
            var uuidType = new UuidType();
            var guid = Guid.NewGuid();

            // act
            var success = uuidType.TryDeserialize(guid, out object? o);

            // assert
            Assert.True(success);
            Assert.Equal(guid, o);
        }

        [Fact]
        public void Deserialize_Int()
        {
            // arrange
            var uuidType = new UuidType();
            var value = 123;

            // act
            var success = uuidType.TryDeserialize(value, out _);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            var uuidType = new UuidType();
            var expected = Guid.NewGuid();
            var literalA = new StringValueNode(expected.ToString("N"));
            var literalB = new StringValueNode(expected.ToString("P"));

            // act
            var runtimeValueA = (Guid)uuidType.ParseLiteral(literalA)!;
            var runtimeValueB = (Guid)uuidType.ParseLiteral(literalB)!;

            // assert
            Assert.Equal(expected, runtimeValueA);
            Assert.Equal(expected, runtimeValueB);
        }

        [Fact]
        public void ParseLiteral_StringValueNode_Enforce_Format()
        {
            // arrange
            var uuidType = new UuidType(defaultFormat: 'P', enforceFormat: true);
            var expected = Guid.NewGuid();
            var literal = new StringValueNode(expected.ToString("N"));

            // act
            void Action() => uuidType.ParseLiteral(literal);

            // assert
            Assert.Throws<SerializationException>(Action);
        }

        [Fact]
        public void ParseLiteral_IntValueNode()
        {
            // arrange
            var uuidType = new UuidType();
            var literal = new IntValueNode(123);

            // act
            void Action() => uuidType.ParseLiteral(literal);

            // assert
            Assert.Throws<SerializationException>(Action);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var uuidType = new UuidType();
            NullValueNode literal = NullValueNode.Default;

            // act
            object? value = uuidType.ParseLiteral(literal);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void ParseLiteral_Null()
        {
            // arrange
            var uuidType = new UuidType();

            // act
            void Action() => uuidType.ParseLiteral(null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void ParseValue_Guid()
        {
            // arrange
            var uuidType = new UuidType();
            var expected = Guid.NewGuid();
            var expectedLiteralValue = expected.ToString("N");

            // act
            var stringLiteral = (StringValueNode)uuidType.ParseValue(expected);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            var uuidType = new UuidType();
            Guid? guid = null;

            // act
            IValueNode stringLiteral = uuidType.ParseValue(guid);

            // assert
            Assert.True(stringLiteral is NullValueNode);
            Assert.Null(((NullValueNode)stringLiteral).Value);
        }

        [Fact]
        public void ParseValue_Int()
        {
            // arrange
            var uuidType = new UuidType();
            var value = 123;

            // act
            void Action() => uuidType.ParseValue(value);

            // assert
            Assert.Throws<SerializationException>(Action);
        }

        [Fact]
        public void EnsureDateTypeKindIsCorret()
        {
            // arrange
            var type = new UuidType();

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }

        [InlineData('N')]
        [InlineData('D')]
        [InlineData('B')]
        [InlineData('P')]
        [Theory]
        public void Serialize_With_Format(char format)
        {
            // arrange
            var uuidType = new UuidType(defaultFormat: format);
            Guid guid = Guid.Empty;

            // act
            string s = (string)uuidType.Serialize(guid)!;

            // assert
            Assert.Equal(guid.ToString(format.ToString()), s);
        }

        [InlineData('N')]
        [InlineData('D')]
        [InlineData('B')]
        [InlineData('P')]
        [Theory]
        public void Deserialize_With_Format(char format)
        {
            // arrange
            var uuidType = new UuidType(defaultFormat: format);
            Guid guid = Guid.Empty;
            string serialized = guid.ToString(format.ToString());

            // act
            var deserialized = (Guid)uuidType.Deserialize(serialized)!;

            // assert
            Assert.Equal(guid, deserialized);
        }

        [InlineData('N')]
        [InlineData('D')]
        [InlineData('B')]
        [InlineData('P')]
        [Theory]
        public void ParseValue_With_Format(char format)
        {
            // arrange
            var uuidType = new UuidType(defaultFormat: format);
            Guid guid = Guid.Empty;

            // act
            var s = (StringValueNode)uuidType.ParseValue(guid);

            // assert
            Assert.Equal(guid.ToString(format.ToString()), s.Value);
        }

        [InlineData('N')]
        [InlineData('D')]
        [InlineData('B')]
        [InlineData('P')]
        [Theory]
        public void ParseLiteral_With_Format(char format)
        {
            // arrange
            var uuidType = new UuidType(defaultFormat: format);
            Guid guid = Guid.Empty;
            var literal = new StringValueNode(guid.ToString(format.ToString()));

            // act
            var deserialized = (Guid)uuidType.ParseLiteral(literal)!;

            // assert
            Assert.Equal(guid, deserialized);
        }

        [Fact]
        public void Specify_Invalid_Format()
        {
            // arrange
            // act
            void Action() => new UuidType(defaultFormat: 'z');

            // assert
            #if NETCOREAPP2_1
            Assert.Throws<ArgumentException>(Action).Message
                .MatchSnapshot(new SnapshotNameExtension("NETCOREAPP2_1"));
            #else
            Assert.Throws<ArgumentException>(Action).Message.MatchSnapshot();
            #endif
        }
    }
}
