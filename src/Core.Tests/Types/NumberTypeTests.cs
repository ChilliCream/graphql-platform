using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public abstract class NumberTypeTests<TNative, TType, TValueNode>
        where TType : ScalarType, new()
        where TValueNode : IValueNode<string>
    {
        protected abstract TValueNode CreateValueNode();
        protected abstract IValueNode CreateWrongValueNode();
        protected abstract TNative CreateValue();
        protected abstract object CreateWrongValue();
        protected abstract TNative AssertValue();
        protected abstract TNative CreateMaxValue();
        protected abstract string AssertMaxValue();
        protected abstract TNative CreateMinValue();
        protected abstract string AssertMinValue();

        [Fact]
        public void IsInstanceOfType_ValueNode()
        {
            // arrange
            TType type = new TType();
            IValueNode input = CreateValueNode();

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
            NullValueNode input = new NullValueNode();

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
            IValueNode input = CreateWrongValueNode();

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
            TNative input = CreateValue();

            // act
            object serializedValue = type.Serialize(input);

            // assert
            Assert.IsType<TNative>(serializedValue);
            Assert.Equal(AssertValue(), serializedValue);
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
            object input = CreateWrongValue();

            // act
            // assert
            Assert.Throws<ArgumentException>(() => type.Serialize(input));
        }

        [Fact]
        public void ParseLiteral_ValueNode()
        {
            // arrange
            TType type = new TType();
            IValueNode input = CreateValueNode();

            // act
            object output = type.ParseLiteral(input);

            // assert
            Assert.IsType<TNative>(output);
            Assert.Equal(AssertValue(), output);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            TType type = new TType();
            NullValueNode input = new NullValueNode();

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
            IValueNode input = CreateWrongValueNode();

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
            TNative input = CreateMaxValue();

            // act
            TValueNode literal =
                (TValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(AssertMaxValue(), literal.Value);
        }

        [Fact]
        public void ParseValue_Min()
        {
            // arrange
            TType type = new TType();
            TNative input = CreateMinValue();

            // act
            TValueNode literal =
                (TValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(AssertMinValue(), literal.Value);
        }

        [Fact]
        public void ParseValue_Wrong_Value_Throws()
        {
            // arrange
            TType type = new TType();
            object input = CreateWrongValue();

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