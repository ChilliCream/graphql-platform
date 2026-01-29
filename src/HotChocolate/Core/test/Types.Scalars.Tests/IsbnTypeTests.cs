using HotChocolate.Language;

namespace HotChocolate.Types;

public class IsbnTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<IsbnType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "ISBN 978-0-596-52068-7", "ISBN 978-0-596-52068-7")]
    [InlineData(typeof(StringValueNode), "ISBN-13: 978-0-596-52068-7", "ISBN-13: 978-0-596-52068-7")]
    [InlineData(typeof(StringValueNode), "978 0 596 52068 7", "978 0 596 52068 7")]
    [InlineData(typeof(StringValueNode), "9780596520687", "9780596520687")]
    [InlineData(typeof(StringValueNode), "ISBN-10 0-596-52068-9", "ISBN-10 0-596-52068-9")]
    [InlineData(typeof(StringValueNode), "0-596-52068-9", "0-596-52068-9")]
    [InlineData(typeof(StringValueNode), "ISBN: 9780615856", "ISBN: 9780615856")]
    [InlineData(typeof(StringValueNode), "1577677171", "1577677171")]
    [InlineData(typeof(StringValueNode), "978 787 78 78", "978 787 78 78")]
    [InlineData(typeof(StringValueNode), "9790765335", "9790765335")]
    [InlineData(typeof(StringValueNode), "979076533X", "979076533X")]
    [InlineData(typeof(StringValueNode), "9780615856", "9780615856")]
    [InlineData(typeof(StringValueNode), "ISBN 978-0615-856-73-5", "ISBN 978-0615-856-73-5")]
    [InlineData(typeof(StringValueNode), "ISBN-13: 978-0615-856-73-5", "ISBN-13: 978-0615-856-73-5")]
    [InlineData(typeof(StringValueNode), "ISBN-13: 9780765335999", "ISBN-13: 9780765335999")]
    [InlineData(typeof(StringValueNode), "ISBN: 9780615856735", "ISBN: 9780615856735")]
    [InlineData(typeof(StringValueNode), "978-0615-856-73-5", "978-0615-856-73-5")]
    [InlineData(typeof(StringValueNode), "9780765335999", "9780765335999")]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToMatch<IsbnType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(IntValueNode), 12345)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(StringValueNode), "1")]
    [InlineData(typeof(StringValueNode), "0-200-xxxxx-x")]
    [InlineData(typeof(StringValueNode), "1-714-2x4x3-x")]
    [InlineData(typeof(StringValueNode), "0-6480000-x-x")]
    [InlineData(typeof(StringValueNode), "0-9999999-x-x")]
    [InlineData(typeof(StringValueNode), "1-7320000-x-x")]
    [InlineData(typeof(StringValueNode), "1-915999-xx-x")]
    [InlineData(typeof(StringValueNode), "1-86719-xxx-x")]
    [InlineData(typeof(StringValueNode), "ISBN 1-7320000-x-8")]
    [InlineData(typeof(StringValueNode), "ISBN1-915999-87-x")]
    [InlineData(typeof(StringValueNode), "ISBN:131-86719-xxx-x")]
    [InlineData(typeof(StringValueNode), "ISBN 9718-0615-856-73-5")]
    [InlineData(typeof(StringValueNode), "ISBN: X9780615856735")]
    [InlineData(typeof(StringValueNode), "ISBN-13: 978-0615-56-73-5")]
    [InlineData(typeof(StringValueNode), "ISBN-13: 9780X765335999")]
    public void CoerceInputLiteral_GivenValueNode_Throw(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToThrow<IsbnType>(valueNode);
    }

    [Theory]
    [InlineData("\"ISBN 978-0-596-52068-7\"", "ISBN 978-0-596-52068-7")]
    [InlineData("\"ISBN-13: 978-0-596-52068-7\"", "ISBN-13: 978-0-596-52068-7")]
    [InlineData("\"978 0 596 52068 7\"", "978 0 596 52068 7")]
    [InlineData("\"9780596520687\"", "9780596520687")]
    [InlineData("\"ISBN-10 0-596-52068-9\"", "ISBN-10 0-596-52068-9")]
    [InlineData("\"0-596-52068-9\"", "0-596-52068-9")]
    [InlineData("\"ISBN: 9780615856\"", "ISBN: 9780615856")]
    [InlineData("\"1577677171\"", "1577677171")]
    [InlineData("\"978 787 78 78\"", "978 787 78 78")]
    [InlineData("\"9790765335\"", "9790765335")]
    [InlineData("\"979076533X\"", "979076533X")]
    [InlineData("\"9780615856\"", "9780615856")]
    [InlineData("\"ISBN 978-0615-856-73-5\"", "ISBN 978-0615-856-73-5")]
    [InlineData("\"ISBN-13: 978-0615-856-73-5\"", "ISBN-13: 978-0615-856-73-5")]
    [InlineData("\"ISBN-13: 9780765335999\"", "ISBN-13: 9780765335999")]
    [InlineData("\"ISBN: 9780615856735\"", "ISBN: 9780615856735")]
    [InlineData("\"978-0615-856-73-5\"", "978-0615-856-73-5")]
    [InlineData("\"9780765335999\"", "9780765335999")]
    public void CoerceInputValue_GivenValue_MatchExpected(
        string jsonValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToMatch<IsbnType>(jsonValue, runtimeValue);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("12345")]
    [InlineData("\"\"")]
    [InlineData("\"1\"")]
    [InlineData("\"0-200-xxxxx-x\"")]
    [InlineData("\"1-714-2x4x3-x\"")]
    [InlineData("\"0-6480000-x-x\"")]
    [InlineData("\"0-9999999-x-x\"")]
    [InlineData("\"1-7320000-x-x\"")]
    [InlineData("\"1-915999-xx-x\"")]
    [InlineData("\"1-86719-xxx-x\"")]
    [InlineData("\"ISBN 1-7320000-x-8\"")]
    [InlineData("\"ISBN1-915999-87-x\"")]
    [InlineData("\"ISBN:131-86719-xxx-x\"")]
    [InlineData("\"ISBN 9718-0615-856-73-5\"")]
    [InlineData("\"ISBN: X9780615856735\"")]
    [InlineData("\"ISBN-13: 978-0615-56-73-5\"")]
    [InlineData("\"ISBN-13: 9780X765335999\"")]
    public void CoerceInputValue_GivenValue_Throw(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrow<IsbnType>(jsonValue);
    }

    [Theory]
    [InlineData("ISBN 978-0-596-52068-7")]
    [InlineData("ISBN-13: 978-0-596-52068-7")]
    [InlineData("978 0 596 52068 7")]
    [InlineData("9780596520687")]
    [InlineData("ISBN-10 0-596-52068-9")]
    [InlineData("0-596-52068-9")]
    [InlineData("ISBN: 9780615856")]
    [InlineData("1577677171")]
    [InlineData("978 787 78 78")]
    [InlineData("9790765335")]
    [InlineData("979076533X")]
    [InlineData("9780615856")]
    [InlineData("ISBN 978-0615-856-73-5")]
    [InlineData("ISBN-13: 978-0615-856-73-5")]
    [InlineData("ISBN-13: 9780765335999")]
    [InlineData("ISBN: 9780615856735")]
    [InlineData("978-0615-856-73-5")]
    [InlineData("9780765335999")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToMatch<IsbnType>(runtimeValue);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("0-200-xxxxx-x")]
    [InlineData("1-714-2x4x3-x")]
    [InlineData("0-6480000-x-x")]
    [InlineData("0-9999999-x-x")]
    [InlineData("1-7320000-x-x")]
    [InlineData("1-915999-xx-x")]
    [InlineData("1-86719-xxx-x")]
    [InlineData("ISBN 1-7320000-x-8")]
    [InlineData("ISBN1-915999-87-x")]
    [InlineData("ISBN:131-86719-xxx-x")]
    [InlineData("ISBN 9718-0615-856-73-5")]
    [InlineData("ISBN: X9780615856735")]
    [InlineData("ISBN-13: 978-0615-56-73-5")]
    [InlineData("ISBN-13: 9780X765335999")]
    public void CoerceOutputValue_GivenObject_Throw(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrow<IsbnType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "ISBN-13: 978-0-596-52068-7")]
    [InlineData(typeof(StringValueNode), "978 0 596 52068 7")]
    [InlineData(typeof(StringValueNode), "9780596520687")]
    [InlineData(typeof(StringValueNode), "ISBN-10 0-596-52068-9")]
    [InlineData(typeof(StringValueNode), "0-596-52068-9")]
    [InlineData(typeof(StringValueNode), "ISBN: 9780615856")]
    [InlineData(typeof(StringValueNode), "1577677171")]
    [InlineData(typeof(StringValueNode), "978 787 78 78")]
    [InlineData(typeof(StringValueNode), "9790765335")]
    [InlineData(typeof(StringValueNode), "979076533X")]
    [InlineData(typeof(StringValueNode), "9780615856")]
    [InlineData(typeof(StringValueNode), "ISBN 978-0615-856-73-5")]
    [InlineData(typeof(StringValueNode), "ISBN-13: 978-0615-856-73-5")]
    [InlineData(typeof(StringValueNode), "ISBN-13: 9780765335999")]
    [InlineData(typeof(StringValueNode), "ISBN: 9780615856735")]
    [InlineData(typeof(StringValueNode), "978-0615-856-73-5")]
    [InlineData(typeof(StringValueNode), "9780765335999")]
    public void ValueToLiteral_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToMatchType<IsbnType>(value, type);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("0-200-xxxxx-x")]
    [InlineData("1-714-2x4x3-x")]
    [InlineData("0-6480000-x-x")]
    [InlineData("0-9999999-x-x")]
    [InlineData("1-7320000-x-x")]
    [InlineData("1-915999-xx-x")]
    [InlineData("1-86719-xxx-x")]
    [InlineData("ISBN 1-7320000-x-8")]
    [InlineData("ISBN1-915999-87-x")]
    [InlineData("ISBN:131-86719-xxx-x")]
    [InlineData("ISBN 9718-0615-856-73-5")]
    [InlineData("ISBN: X9780615856735")]
    [InlineData("ISBN-13: 978-0615-56-73-5")]
    [InlineData("ISBN-13: 9780X765335999")]
    public void ValueToLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToThrowSerializationException<IsbnType>(value);
    }
}
