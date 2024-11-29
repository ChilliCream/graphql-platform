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
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(IntValueNode), 1, false)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "978 787 7878", false)]
    [InlineData(typeof(StringValueNode), "978-0615-856", false)]
    [InlineData(typeof(StringValueNode), "978-0615856735", false)]
    [InlineData(typeof(StringValueNode), "ISBN 978 787 78 78788", false)]
    [InlineData(typeof(StringValueNode), "ISBN 97907653359990", false)]
    [InlineData(typeof(StringValueNode), "ISBN-13: 978-0615856735", false)]
    [InlineData(typeof(StringValueNode), "ISBN: 978-0615-856", false)]
    [InlineData(typeof(StringValueNode), "ISBN 978-0-596-52068-7", true)]
    [InlineData(typeof(StringValueNode), "ISBN-13: 978-0-596-52068-7", true)]
    [InlineData(typeof(StringValueNode), "978 0 596 52068 7", true)]
    [InlineData(typeof(StringValueNode), "9780596520687", true)]
    [InlineData(typeof(StringValueNode), "ISBN-10 0-596-52068-9", true)]
    [InlineData(typeof(StringValueNode), "0-596-52068-9", true)]
    [InlineData(typeof(StringValueNode), "ISBN: 9780615856", true)]
    [InlineData(typeof(StringValueNode), "1577677171", true)]
    [InlineData(typeof(StringValueNode), "978 787 78 78", true)]
    [InlineData(typeof(StringValueNode), "9790765335", true)]
    [InlineData(typeof(StringValueNode), "979076533X", true)]
    [InlineData(typeof(StringValueNode), "9780615856", true)]
    [InlineData(typeof(StringValueNode), "ISBN 978-0615-856-73-5", true)]
    [InlineData(typeof(StringValueNode), "ISBN-13: 978-0615-856-73-5", true)]
    [InlineData(typeof(StringValueNode), "ISBN-13: 9780765335999", true)]
    [InlineData(typeof(StringValueNode), "ISBN: 9780615856735", true)]
    [InlineData(typeof(StringValueNode), "978-0615-856-73-5", true)]
    [InlineData(typeof(StringValueNode), "9780765335999", true)]
    [InlineData(typeof(NullValueNode), null, true)]
    public void IsInstanceOfType_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        bool expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<IsbnType>(valueNode, expected);
    }

    [Theory]
    [InlineData(TestEnum.Foo, false)]
    [InlineData(1d, false)]
    [InlineData(1, false)]
    [InlineData(true, false)]
    [InlineData("", false)]
    [InlineData("978 787 7878", false)]
    [InlineData("978-0615-856", false)]
    [InlineData("978-0615856735", false)]
    [InlineData("ISBN 978 787 78 78788", false)]
    [InlineData("ISBN 97907653359990", false)]
    [InlineData("ISBN-13: 978-0615856735", false)]
    [InlineData("ISBN: 978-0615-856", false)]
    [InlineData("ISBN 978-0-596-52068-7", true)]
    [InlineData("ISBN-13: 978-0-596-52068-7", true)]
    [InlineData("978 0 596 52068 7", true)]
    [InlineData("9780596520687", true)]
    [InlineData("ISBN-10 0-596-52068-9", true)]
    [InlineData("0-596-52068-9", true)]
    [InlineData("ISBN: 9780615856", true)]
    [InlineData("1577677171", true)]
    [InlineData("978 787 78 78", true)]
    [InlineData("9790765335", true)]
    [InlineData("979076533X", true)]
    [InlineData("9780615856", true)]
    [InlineData("ISBN 978-0615-856-73-5", true)]
    [InlineData("ISBN-13: 978-0615-856-73-5", true)]
    [InlineData("ISBN-13: 9780765335999", true)]
    [InlineData("ISBN: 9780615856735", true)]
    [InlineData("978-0615-856-73-5", true)]
    [InlineData("9780765335999", true)]
    [InlineData(null, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<IsbnType>(value, expected);
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
    [InlineData(typeof(NullValueNode), null, null)]
    public void ParseLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToMatch<IsbnType>(valueNode, expected);
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
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<IsbnType>(valueNode);
    }

    [Theory]
    [InlineData("ISBN 978-0-596-52068-7", "ISBN 978-0-596-52068-7")]
    [InlineData("ISBN-13: 978-0-596-52068-7", "ISBN-13: 978-0-596-52068-7")]
    [InlineData("978 0 596 52068 7", "978 0 596 52068 7")]
    [InlineData("9780596520687", "9780596520687")]
    [InlineData("ISBN-10 0-596-52068-9", "ISBN-10 0-596-52068-9")]
    [InlineData("0-596-52068-9", "0-596-52068-9")]
    [InlineData("ISBN: 9780615856", "ISBN: 9780615856")]
    [InlineData("1577677171", "1577677171")]
    [InlineData("978 787 78 78", "978 787 78 78")]
    [InlineData("9790765335", "9790765335")]
    [InlineData("979076533X", "979076533X")]
    [InlineData("9780615856", "9780615856")]
    [InlineData("ISBN 978-0615-856-73-5", "ISBN 978-0615-856-73-5")]
    [InlineData("ISBN-13: 978-0615-856-73-5", "ISBN-13: 978-0615-856-73-5")]
    [InlineData("ISBN-13: 9780765335999", "ISBN-13: 9780765335999")]
    [InlineData("ISBN: 9780615856735", "ISBN: 9780615856735")]
    [InlineData("978-0615-856-73-5", "978-0615-856-73-5")]
    [InlineData("9780765335999", "9780765335999")]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<IsbnType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
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
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<IsbnType>(value);
    }

    [Theory]
    [InlineData("ISBN 978-0-596-52068-7", "ISBN 978-0-596-52068-7")]
    [InlineData("ISBN-13: 978-0-596-52068-7", "ISBN-13: 978-0-596-52068-7")]
    [InlineData("978 0 596 52068 7", "978 0 596 52068 7")]
    [InlineData("9780596520687", "9780596520687")]
    [InlineData("ISBN-10 0-596-52068-9", "ISBN-10 0-596-52068-9")]
    [InlineData("0-596-52068-9", "0-596-52068-9")]
    [InlineData("ISBN: 9780615856", "ISBN: 9780615856")]
    [InlineData("1577677171", "1577677171")]
    [InlineData("978 787 78 78", "978 787 78 78")]
    [InlineData("9790765335", "9790765335")]
    [InlineData("979076533X", "979076533X")]
    [InlineData("9780615856", "9780615856")]
    [InlineData("ISBN 978-0615-856-73-5", "ISBN 978-0615-856-73-5")]
    [InlineData("ISBN-13: 978-0615-856-73-5", "ISBN-13: 978-0615-856-73-5")]
    [InlineData("ISBN-13: 9780765335999", "ISBN-13: 9780765335999")]
    [InlineData("ISBN: 9780615856735", "ISBN: 9780615856735")]
    [InlineData("978-0615-856-73-5", "978-0615-856-73-5")]
    [InlineData("9780765335999", "9780765335999")]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<IsbnType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
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
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<IsbnType>(value);
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
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<IsbnType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
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
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<IsbnType>(value);
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
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<IsbnType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
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
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<IsbnType>(value);
    }
}
