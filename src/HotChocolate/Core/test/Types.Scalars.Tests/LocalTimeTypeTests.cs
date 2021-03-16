using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Scalars
{
    public class LocalTimeTypeTests : ScalarTypeTestBase
    {
        [Fact]
        public void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<LocalTimeType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Theory]
        [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
        [InlineData(typeof(FloatValueNode), 1d, false)]
        [InlineData(typeof(IntValueNode), 1, false)]
        [InlineData(typeof(BooleanValueNode), true, false)]
        [InlineData(typeof(StringValueNode), "31.10.2008", true)]
        [InlineData(typeof(StringValueNode), "10/31/2008", true)]
        [InlineData(typeof(StringValueNode), "31/10/2008", true)]
        [InlineData(typeof(StringValueNode), "Freitag, 31. Oktober 2008", true)]
        [InlineData(typeof(StringValueNode), "Friday, October 31, 2008", true)]
        [InlineData(typeof(StringValueNode), "viernes, 31 de octubre de 2008", true)]
        [InlineData(typeof(StringValueNode), "vendredi 31 octobre 2008", true)]
        [InlineData(typeof(StringValueNode), "Freitag, 31. Oktober 2008 17:04", true)]
        [InlineData(typeof(StringValueNode), "Friday, October 31, 2008 5:04 PM", true)]
        [InlineData(typeof(StringValueNode), "viernes, 31 de octubre de 2008 17:04", true)]
        [InlineData(typeof(StringValueNode), "vendredi 31 octobre 2008 17:04", true)]
        [InlineData(typeof(StringValueNode), "Freitag, 31. Oktober 2008 17:04:32", true)]
        [InlineData(typeof(StringValueNode), "Friday, October 31, 2008 5:04:32 PM", true)]
        [InlineData(typeof(StringValueNode), "viernes, 31 de octubre de 2008 17:04:32", true)]
        [InlineData(typeof(StringValueNode), "vendredi 31 octobre 2008 17:04:32", true)]
        [InlineData(typeof(StringValueNode), "31.10.2008 17:04", true)]
        [InlineData(typeof(StringValueNode), "10/31/2008 5:04 PM", true)]
        [InlineData(typeof(StringValueNode), "31/10/2008 17:04", true)]
        [InlineData(typeof(StringValueNode), "31.10.2008 17:04:32", true)]
        [InlineData(typeof(StringValueNode), "10/31/2008 5:04:32 PM", true)]
        [InlineData(typeof(StringValueNode), "31/10/2008 17:04:32", true)]
        [InlineData(typeof(StringValueNode), "31. Oktober", true)]
        [InlineData(typeof(StringValueNode), "October 31", true)]
        [InlineData(typeof(StringValueNode), "31 de octubre", true)]
        [InlineData(typeof(StringValueNode), "31 octobre", true)]
        [InlineData(typeof(StringValueNode), "2008-10-31T17:04:32.0000000", true)]
        [InlineData(typeof(StringValueNode), "Fri, 31 Oct 2008 17:04:32 GMT", true)]
        [InlineData(typeof(StringValueNode), "2008-10-31T17:04:32", true)]
        [InlineData(typeof(StringValueNode), "17:04", true)]
        [InlineData(typeof(StringValueNode), "5:04 PM", true)]
        [InlineData(typeof(StringValueNode), "17:04:32", true)]
        [InlineData(typeof(StringValueNode), "5:04:32 PM", true)]
        [InlineData(typeof(StringValueNode), "2008-10-31 17:04:32Z", true)]
        [InlineData(typeof(StringValueNode), "Freitag, 31. Oktober 2008 09:04:32", true)]
        [InlineData(typeof(StringValueNode), "viernes, 31 de octubre de 2008 9:04:32", true)]
        [InlineData(typeof(StringValueNode), "vendredi 31 octobre 2008 09:04:32", true)]
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
            ExpectIsInstanceOfTypeToMatch<LocalTimeType>(valueNode, expected);
        }
    }
}
