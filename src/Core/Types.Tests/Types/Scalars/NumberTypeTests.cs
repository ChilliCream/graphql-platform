using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public abstract class NumberTypeTests<TClr, TScalar, TLiteral, TSerialized>
        where TScalar : ScalarType, new()
        where TLiteral : IValueNode<string>
    {
        protected abstract TLiteral GetValueNode { get; }
        protected abstract IValueNode GetWrongValueNode { get; }
        protected abstract TClr GetValue { get; }
        protected abstract object GetWrongValue { get; }
        protected abstract TClr GetAssertValue { get; }
        protected abstract TClr GetMaxValue { get; }
        protected abstract TSerialized GetSerializedAssertValue { get; }
        protected abstract string GetAssertMaxValue { get; }
        protected abstract TClr GetMinValue { get; }
        protected abstract string GetAssertMinValue { get; }

        [Fact]
        public void IsInstanceOfType_ValueNode()
        {
            // arrange
            var type = new TScalar();
            IValueNode input = GetValueNode;

            // act
            var result = type.IsInstanceOfType(input);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_NullValueNode()
        {
            // arrange
            var type = new TScalar();
            NullValueNode input = NullValueNode.Default;

            // act
            var result = type.IsInstanceOfType(input);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_Wrong_ValueNode()
        {
            // arrange
            var type = new TScalar();
            IValueNode input = GetWrongValueNode;

            // act
            var result = type.IsInstanceOfType(input);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_Null_Throws()
        {
            // arrange
            var type = new TScalar();

            // act
            // assert
            Assert.Throws<ArgumentNullException>(
                () => type.IsInstanceOfType(null));
        }

        [Fact]
        public void Serialize_Type()
        {
            // arrange
            var type = new TScalar();
            TClr input = GetValue;

            // act
            var serializedValue = type.Serialize(input);

            // assert
            Assert.IsType<TSerialized>(serializedValue);
            Assert.Equal(GetSerializedAssertValue, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var type = new TScalar();

            // act
            var serializedValue = type.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_Wrong_Type_Throws()
        {
            // arrange
            var type = new TScalar();
            var input = GetWrongValue;

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.Serialize(input));
        }

        [Fact]
        public void ParseLiteral_ValueNode()
        {
            // arrange
            var type = new TScalar();
            IValueNode input = GetValueNode;

            // act
            var output = type.ParseLiteral(input);

            // assert
            Assert.IsType<TClr>(output);
            Assert.Equal(GetAssertValue, output);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var type = new TScalar();
            NullValueNode input = NullValueNode.Default;

            // act
            var output = type.ParseLiteral(input);

            // assert
            Assert.Null(output);
        }

        [Fact]
        public void ParseLiteral_Wrong_ValueNode_Throws()
        {
            // arrange
            var type = new TScalar();
            IValueNode input = GetWrongValueNode;

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.ParseLiteral(input));
        }

        [Fact]
        public void ParseLiteral_Null_Throws()
        {
            // arrange
            var type = new TScalar();

            // act
            // assert
            Assert.Throws<ArgumentNullException>(
                () => type.ParseLiteral(null));
        }

        [Fact]
        public void ParseValue_Max()
        {
            // arrange
            var type = new TScalar();
            TClr input = GetMaxValue;

            // act
            var literal =
                (TLiteral)type.ParseValue(input);

            // assert
            Assert.Equal(GetAssertMaxValue, literal.Value);
        }

        [Fact]
        public void ParseValue_Min()
        {
            // arrange
            var type = new TScalar();
            TClr input = GetMinValue;

            // act
            var literal =
                (TLiteral)type.ParseValue(input);

            // assert
            Assert.Equal(GetAssertMinValue, literal.Value);
        }

        [Fact]
        public void ParseValue_Wrong_Value_Throws()
        {
            // arrange
            var type = new TScalar();
            var input = GetWrongValue;

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.ParseValue(input));
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            var type = new TScalar();
            object input = null;

            // act
            object output = type.ParseValue(input);

            // assert
            Assert.IsType<NullValueNode>(output);
        }


        [Fact]
        public void Ensure_TypeKind()
        {
            // arrange
            var type = new TScalar();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, kind);
        }
    }
}
