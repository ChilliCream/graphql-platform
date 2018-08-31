using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class InputCoercionTests
    {
        public void Boolean_True_True()
        {
            // arrange
            var type = new BooleanType();
            var value = new BooleanValueNode(true);

            // act



            // assert
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
    }
}


it('converts according to input coercion rules', () => {
    testCase(GraphQLBoolean, 'true', true);
    testCase(GraphQLBoolean, 'false', false);
    testCase(GraphQLInt, '123', 123);
    testCase(GraphQLFloat, '123', 123);
    testCase(GraphQLFloat, '123.456', 123.456);
    testCase(GraphQLString, '"abc123"', 'abc123');
    testCase(GraphQLID, '123456', '123456');
    testCase(GraphQLID, '"123456"', '123456');
});

  it('does not convert when input coercion rules reject a value', () => {
    testCase(GraphQLBoolean, '123', undefined);
    testCase(GraphQLInt, '123.456', undefined);
    testCase(GraphQLInt, 'true', undefined);
    testCase(GraphQLInt, '"123"', undefined);
    testCase(GraphQLFloat, '"123"', undefined);
    testCase(GraphQLString, '123', undefined);
    testCase(GraphQLString, 'true', undefined);
    testCase(GraphQLID, '123.456', undefined);
});
