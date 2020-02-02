using System;
using System.Collections.Generic;
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
                new IntValueNode(123), 123);
            InputIsCoercedCorrectly<FloatType, IntValueNode, double>(
                new IntValueNode(123), 123d);
            InputIsCoercedCorrectly<FloatType, FloatValueNode, double>(
                new FloatValueNode(123.456d), 123.456d);
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
                new IntValueNode(123));
            InputCannotBeCoercedCorrectly<IntType, FloatValueNode>(
                new FloatValueNode(123.123d));
            InputCannotBeCoercedCorrectly<IntType, BooleanValueNode>(
                new BooleanValueNode(true));
            InputCannotBeCoercedCorrectly<IntType, StringValueNode>(
                new StringValueNode("123.123"));
            InputCannotBeCoercedCorrectly<FloatType, StringValueNode>(
                new StringValueNode("123"));
            InputCannotBeCoercedCorrectly<StringType, FloatValueNode>(
                new FloatValueNode(123.456d));
            InputCannotBeCoercedCorrectly<StringType, BooleanValueNode>(
                new BooleanValueNode(false));
            InputIsCoercedCorrectly<IdType, StringValueNode, string>(
                new StringValueNode("123456"), "123456");
        }

        [Fact]
        public void InputListIsInstanceOf()
        {
            InputListIsInstanceOfInternal<BooleanType>(
                new ListValueNode(new BooleanValueNode(true)));
            InputListIsInstanceOfInternal<BooleanType>(
                new BooleanValueNode(true));

            InputListIsNotInstanceOfInternal<BooleanType>(
                new ListValueNode(new IValueNode[] {
                    new BooleanValueNode(true),
                    new StringValueNode("123") }));
            InputListIsNotInstanceOfInternal<BooleanType>(
                new StringValueNode("123"));
        }

        [Fact]
        public void ListCanBeCoercedFromListValue()
        {
            // arrange
            var type = (IInputType)new ListType(new BooleanType());
            var list = new ListValueNode(
                new[] {
                    new BooleanValueNode(true),
                    new BooleanValueNode(false)});

            // act
            object coercedValue = type.ParseLiteral(list);

            // assert
            Assert.Collection(
                Assert.IsType<List<bool?>>(coercedValue),
                t => Assert.True(t),
                t => Assert.False(t));
        }

        [Fact]
        public void ListCanBeCoercedFromListElementValue()
        {
            // arrange
            var type = (IInputType)new ListType(new BooleanType());
            var element = new BooleanValueNode(true);

            // act
            object coercedValue = type.ParseLiteral(element);

            // assert
            Assert.Collection(
                Assert.IsType<List<bool?>>(coercedValue),
                t => Assert.True(t));
        }

        [Fact]
        public void ListCannotBeCoercedFromMixedList()
        {
            // arrange
            var type = (IInputType)new ListType(new BooleanType());
            var list = new ListValueNode(
                new IValueNode[] {
                    new BooleanValueNode(true),
                    new StringValueNode("foo")});

            // act
            Action action = () => type.ParseLiteral(list);

            // assert
            Assert.Throws<ScalarSerializationException>(action);
        }

        [Fact]
        public void ListCannotBeCoercedIfElementTypeDoesNotMatch()
        {
            // arrange
            var type = (IInputType)new ListType(new BooleanType());
            var element = new StringValueNode("foo");

            // act
            Action action = () => type.ParseLiteral(element);

            // assert
            Assert.Throws<ArgumentException>(action);
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
            Assert.Throws<ScalarSerializationException>(action);
        }

        private void InputListIsInstanceOfInternal<TElement>(
           IValueNode literal)
           where TElement : ScalarType, new()
        {
            // arrange
            var type = (IInputType)new ListType(new TElement());

            // act
            bool isInstanceOfType = type.IsInstanceOfType(literal);

            // assert
            Assert.True(isInstanceOfType);
        }

        private void InputListIsNotInstanceOfInternal<TElement>(
           IValueNode literal)
           where TElement : ScalarType, new()
        {
            // arrange
            var type = (IInputType)new ListType(new TElement());

            // act
            bool isInstanceOfType = type.IsInstanceOfType(literal);

            // assert
            Assert.False(isInstanceOfType);
        }
    }
}
