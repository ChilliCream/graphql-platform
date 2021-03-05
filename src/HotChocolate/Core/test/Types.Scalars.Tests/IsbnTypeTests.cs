using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Scalars
{
    public class IsbnTypeTests : ScalarTypeTestBase
    {
        [Fact]
        public void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<IsbnType>();

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
        [InlineData(typeof(StringValueNode), "ISBN 1561616161", true)]
        [InlineData(typeof(StringValueNode), "ISBN 1577677171", true)]
        [InlineData(typeof(StringValueNode), "ISBN 978 787 78 78", true)]
        [InlineData(typeof(StringValueNode), "ISBN 978 787 7878", true)]
        [InlineData(typeof(StringValueNode), "ISBN 978-0615-856", true)]
        [InlineData(typeof(StringValueNode), "ISBN 9790765335", true)]
        [InlineData(typeof(StringValueNode), "ISBN 979076533X", true)]
        [InlineData(typeof(StringValueNode), "ISBN: 978-0615-856", true)]
        [InlineData(typeof(StringValueNode), "ISBN: 9780615856", true)]
        [InlineData(typeof(StringValueNode), "1577677171", true)]
        [InlineData(typeof(StringValueNode), "978 787 78 78", true)]
        [InlineData(typeof(StringValueNode), "978 787 7878", true)]
        [InlineData(typeof(StringValueNode), "978-0615-856", true)]
        [InlineData(typeof(StringValueNode), "9790765335", false)]
        [InlineData(typeof(StringValueNode), "979076533X", true)]
        [InlineData(typeof(StringValueNode), "9780615856", true)]
        [InlineData(typeof(StringValueNode), "ISBN 978 787 78 78788", true)]
        [InlineData(typeof(StringValueNode), "ISBN 978-0615-856-73-5", true)]
        [InlineData(typeof(StringValueNode), "ISBN 97907653359990", true)]
        [InlineData(typeof(StringValueNode), "ISBN-13: 978-0615-856-73-5", true)]
        [InlineData(typeof(StringValueNode), "ISBN-13: 978-0615856735", true)]
        [InlineData(typeof(StringValueNode), "ISBN-13: 9780765335999", true)]
        [InlineData(typeof(StringValueNode), "ISBN: 9780615856735", false)]
        [InlineData(typeof(StringValueNode), "978-0615-856-73-5", true)]
        [InlineData(typeof(StringValueNode), "978-0615856735", true)]
        [InlineData(typeof(StringValueNode), "9780765335999", true)]
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
            ExpectIsInstanceOfTypeToMatch<IsbnType>(valueNode, expected);
        }
    }
}
