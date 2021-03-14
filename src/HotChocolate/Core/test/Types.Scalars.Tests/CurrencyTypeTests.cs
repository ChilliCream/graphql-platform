using System;
using System.Threading.Tasks;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;


namespace HotChocolate.Types.Scalars
{
    public class CurrencyTypeTests : ScalarTypeTestBase
    {
        [Fact]
        public void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<CurrencyType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Theory]
        [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
        [InlineData(typeof(FloatValueNode), 1d, false)]
        [InlineData(typeof(IntValueNode), 1, false)]
        [InlineData(typeof(BooleanValueNode), true, false)]
        [InlineData(typeof(StringValueNode), "", false)]
        [InlineData(typeof(StringValueNode), "x", false)]
        [InlineData(typeof(StringValueNode), "19", false)]
        [InlineData(typeof(StringValueNode), "EUR 1888.66", false)] //whitespace
        [InlineData(typeof(StringValueNode), "EUR 1888.6", false)] //hard-space
        [InlineData(typeof(StringValueNode), "EUR 1888.66", true)]
        [InlineData(typeof(StringValueNode), "EUR 1888,66", true)] // mix sep
        [InlineData(typeof(StringValueNode), "USD 1,771,888.66", true)]
        [InlineData(typeof(StringValueNode), "USD 1.771.888,66", true)]
        [InlineData(typeof(StringValueNode), "UDD 1,771,888.66", false)] // not active currency
        [InlineData(typeof(StringValueNode), "USD 1,7711,888.66", false)]
        [InlineData(typeof(StringValueNode), "$ 1,776,888.6", false)] // not in ISO-4217
        [InlineData(typeof(StringValueNode), "18000.00 MAD", false)] // not correct separator
        [InlineData(typeof(StringValueNode), "18000,00 MAD", true)]
        [InlineData(typeof(StringValueNode), "18000,0000 MAD", false)]
        [InlineData(typeof(StringValueNode), "784 784.6784", false)]
        [InlineData(typeof(StringValueNode), "784 784.67", true)]
        [InlineData(typeof(StringValueNode), "19.55", false)]
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
            ExpectIsInstanceOfTypeToMatch<CurrencyType>(valueNode, expected);
        }

        [Theory]
        [InlineData("USD 1,711,888.66", "USD 1,711,888.66")]
        [InlineData("USD 1.711.888,66", "USD 1.711.888,66")]
        [InlineData(null, null)]
        public void Deserialize_GivenValue_MatchExpected(
            object resultValue,
            object runtimeValue)
        {
            // arrange
            // act
            // assert
            ExpectDeserializeToMatch<CurrencyType>(resultValue, runtimeValue);
        }

        [Theory]
        [InlineData("USD 1,711,888.66", "USD 1,711,888.66")]
        [InlineData("USD 1.711.888,66", "USD 1.711.888,66")]
        [InlineData(null, null)]
        public void Serialize_GivenObject_MatchExpectedType(
            object runtimeValue,
            object resultValue)
        {
            // arrange
            // act
            // assert
            ExpectSerializeToMatch<CurrencyType>(runtimeValue, resultValue);
        }
    }
}
