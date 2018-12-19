using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public abstract class NumberTypeTests<TNative, TType, TValueNode, TSerialized>
        where TType : ScalarType, new()
        where TValueNode : IValueNode<string>
    {
        protected abstract TValueNode GetValueNode { get; }
        protected abstract IValueNode GetWrongValueNode { get; }
        protected abstract TNative GetValue { get; }
        protected abstract object GetWrongValue { get; }
        protected abstract TNative GetAssertValue { get; }
        protected abstract TNative GetMaxValue { get; }
        protected abstract TSerialized GetSerializedAssertValue { get; }
        protected abstract string GetAssertMaxValue { get; }
        protected abstract TNative GetMinValue { get; }
        protected abstract string GetAssertMinValue { get; }

        [Fact]
        public void IsInstanceOfType_ValueNode()
        {
            // arrange
            TType type = new TType();
            IValueNode input = GetValueNode;

            // act
            bool result = type.IsInstanceOfType(input);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_NullValueNode()
        {
            // arrange
            TType type = new TType();
            NullValueNode input = NullValueNode.Default;

            // act
            bool result = type.IsInstanceOfType(input);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_Wrong_ValueNode()
        {
            // arrange
            TType type = new TType();
            IValueNode input = GetWrongValueNode;

            // act
            bool result = type.IsInstanceOfType(input);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_Null_Throws()
        {
            // arrange
            TType type = new TType();

            // act
            // assert
            Assert.Throws<ArgumentNullException>(() => type.IsInstanceOfType(null));
        }

        [Fact]
        public void Serialize_Type()
        {
            // arrange
            TType type = new TType();
            TNative input = GetValue;

            // act
            object serializedValue = type.Serialize(input);

            // assert
            Assert.IsType<TSerialized>(serializedValue);
            Assert.Equal(GetSerializedAssertValue, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            TType type = new TType();

            // act
            object serializedValue = type.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_Wrong_Type_Throws()
        {
            // arrange
            TType type = new TType();
            object input = GetWrongValue;

            // act
            // assert
            Assert.Throws<ArgumentException>(() => type.Serialize(input));
        }

        [Fact]
        public void ParseLiteral_ValueNode()
        {
            // arrange
            TType type = new TType();
            IValueNode input = GetValueNode;

            // act
            object output = type.ParseLiteral(input);

            // assert
            Assert.IsType<TNative>(output);
            Assert.Equal(GetAssertValue, output);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            TType type = new TType();
            NullValueNode input = NullValueNode.Default;

            // act
            object output = type.ParseLiteral(input);

            // assert
            Assert.Null(output);
        }

        [Fact]
        public void ParseLiteral_Wrong_ValueNode_Throws()
        {
            // arrange
            TType type = new TType();
            IValueNode input = GetWrongValueNode;

            // act
            // assert
            Assert.Throws<ArgumentException>(() => type.ParseLiteral(input));
        }

        [Fact]
        public void ParseLiteral_Null_Throws()
        {
            // arrange
            TType type = new TType();

            // act
            // assert
            Assert.Throws<ArgumentNullException>(() => type.ParseLiteral(null));
        }

        [Fact]
        public void ParseValue_Max()
        {
            // arrange
            TType type = new TType();
            TNative input = GetMaxValue;

            // act
            TValueNode literal =
                (TValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(GetAssertMaxValue, literal.Value);
        }

        [Fact]
        public void ParseValue_Min()
        {
            // arrange
            TType type = new TType();
            TNative input = GetMinValue;

            // act
            TValueNode literal =
                (TValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(GetAssertMinValue, literal.Value);
        }

        [Fact]
        public void ParseValue_Wrong_Value_Throws()
        {
            // arrange
            TType type = new TType();
            object input = GetWrongValue;

            // act
            // assert
            Assert.Throws<ArgumentException>(() => type.ParseValue(input));
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            TType type = new TType();
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
            TType type = new TType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, kind);
        }
    }
}
