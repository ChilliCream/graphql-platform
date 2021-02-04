using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Scalars
{
    public class NegativeFloatTypeTests : ScalarTypeTestBase
    {
        [Fact]
        public void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<NegativeFloatType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Theory]
        [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
        [InlineData(typeof(FloatValueNode), 1d, false)]
        [InlineData(typeof(FloatValueNode), -1.0d, true)]
        [InlineData(typeof(FloatValueNode), 0.00000001d, false)]
        [InlineData(typeof(FloatValueNode), -0.0000001d, true)]
        [InlineData(typeof(FloatValueNode), double.MinValue, true)]
        [InlineData(typeof(IntValueNode), -1, true)]
        [InlineData(typeof(IntValueNode), int.MinValue, true)]
        [InlineData(typeof(IntValueNode), 0, false)]
        [InlineData(typeof(IntValueNode), 1, false)]
        [InlineData(typeof(BooleanValueNode), true, false)]
        [InlineData(typeof(StringValueNode), "", false)]
        [InlineData(typeof(StringValueNode), "foo", false)]
        [InlineData(typeof(NullValueNode), null, true)]
        public void IsInstanceOfType_GivenValueNode_MatchExpected(
            Type type,
            object value,
            bool expected)
        {
            // arrange
            IValueNode valueNode = CreateValueNode(type, value);

            // act
            // assert
            ExpectIsInstanceOfTypeToMatch<NegativeFloatType>(valueNode, expected);
        }
    }
}
