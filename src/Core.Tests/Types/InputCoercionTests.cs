using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class InputCoercionTests
    {
        /// <summary>
        /// Converts according to input coercion rules.
        /// </summary>
        [Fact]
        public void ConvertAccordingToInputCoercionRules()
        {
            InputIsCoercedCorrectly<BooleanType, BooleanValueNode, bool>(
                new BooleanValueNode(true), true);
            InputIsCoercedCorrectly<BooleanType, BooleanValueNode, bool>(
                new BooleanValueNode(false), false);
            InputIsCoercedCorrectly<IntType, IntValueNode, int>(
                new IntValueNode("123"), 123);
            InputIsCoercedCorrectly<FloatType, IntValueNode, double>(
                new IntValueNode("123"), 123d);
            InputIsCoercedCorrectly<FloatType, FloatValueNode, double>(
                new FloatValueNode("123.456"), 123.456d);
            InputIsCoercedCorrectly<StringType, StringValueNode, string>(
                new StringValueNode("abc123"), "abc123");
            InputIsCoercedCorrectly<IdType, StringValueNode, string>(
                new StringValueNode("123456"), "123456");
        }

        /// <summary>
        /// Does not convert when input coercion rules reject a value.
        /// </summary>
        [Fact]
        public void ConvertAccordingToInputCoercionRules2()
        {
            InputCannotBeCoercedCorrectly<BooleanType, IntValueNode>(
                new IntValueNode("123"));
            InputCannotBeCoercedCorrectly<IntType, FloatValueNode>(
                new FloatValueNode("123.123"));
            InputCannotBeCoercedCorrectly<IntType, BooleanValueNode>(
                new BooleanValueNode(true));
            InputCannotBeCoercedCorrectly<IntType, StringValueNode>(
                new StringValueNode("123.123"));
            InputCannotBeCoercedCorrectly<FloatType, StringValueNode>(
                new StringValueNode("123"));
            InputCannotBeCoercedCorrectly<StringType, FloatValueNode>(
                new FloatValueNode("123.456"));
            InputCannotBeCoercedCorrectly<StringType, BooleanValueNode>(
                new BooleanValueNode(false));
            InputIsCoercedCorrectly<IdType, StringValueNode, string>(
                new StringValueNode("123456"), "123456");
        }

        private void InputIsCoercedCorrectly<TType, TLiteral, TExpected>(
            TLiteral literal, TExpected expectedValue)
            where TType : ScalarType, new()
            where TLiteral : IValueNode
        {
            // arrange
            var type = new TType();

            // act
            object coercedValue = type.ParseLiteral(literal);

            // assert
            Assert.IsType<TExpected>(coercedValue);
            Assert.Equal(expectedValue, coercedValue);
        }

        private void InputCannotBeCoercedCorrectly<TType, TLiteral>(
            TLiteral literal)
            where TType : ScalarType, new()
            where TLiteral : IValueNode
        {
            // arrange
            var type = new TType();

            // act
            Action action = () => type.ParseLiteral(literal);

            // assert
            Assert.Throws<ArgumentException>(action);
        }
    }
}
