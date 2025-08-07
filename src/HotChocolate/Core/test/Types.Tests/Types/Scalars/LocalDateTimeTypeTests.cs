using System.Globalization;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class LocalDateTimeTypeTests
{
    [Fact]
    public void Serialize_DateTime()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();
        var dateTime = new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
        const string expectedValue = "2018-06-11T08:46:14";

        // act
        var serializedValue = (string)localDateTimeType.Serialize(dateTime);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_DateTimeOffset()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();
        var dateTime = new DateTimeOffset(
            new DateTime(2018, 6, 11, 2, 46, 14),
            new TimeSpan(4, 0, 0));
        const string expectedValue = "2018-06-11T02:46:14";

        // act
        var serializedValue = (string)localDateTimeType.Serialize(dateTime);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_Null()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();

        // act
        var serializedValue = localDateTimeType.Serialize(null);

        // assert
        Assert.Null(serializedValue);
    }

    [Fact]
    public void Serialize_String_Exception()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();

        // act
        void Action() => localDateTimeType.Serialize("foo");

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void Deserialize_IsoString_DateTime()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();
        var dateTime = new DateTime(2018, 6, 11, 8, 46, 14);

        // act
        var result = (DateTime)localDateTimeType.Deserialize("2018-06-11T08:46:14")!;

        // assert
        Assert.Equal(dateTime, result);
    }

    [Fact]
    public void Deserialize_InvalidFormat_To_DateTime()
    {
        // arrange
        var type = new LocalDateTimeType();

        // act
        var success = type.TryDeserialize("2018/06/11T08:46:14 pm", out _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void Deserialize_InvalidString_To_DateTime()
    {
        // arrange
        var type = new LocalDateTimeType();

        // act
        var success = type.TryDeserialize("abc", out _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void Deserialize_DateTime_To_DateTime()
    {
        // arrange
        var type = new LocalDateTimeType();
        var dateTime = new DateTime(2018, 6, 11, 8, 46, 14);

        // act
        var success = type.TryDeserialize(dateTime, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Equal(dateTime, deserialized);
    }

    [Fact]
    public void Deserialize_DateTimeOffset_To_DateTime()
    {
        // arrange
        var type = new LocalDateTimeType();
        var dateTime = new DateTimeOffset(
            new DateTime(2018, 6, 11, 2, 46, 14),
            new TimeSpan(4, 0, 0));

        // act
        var success = type.TryDeserialize(dateTime, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Equal(dateTime.DateTime, Assert.IsType<DateTime>(deserialized));
    }

    [Fact]
    public void Deserialize_NullableDateTime_To_DateTime()
    {
        // arrange
        var type = new LocalDateTimeType();
        DateTime? dateTime = new DateTime(2018, 6, 11, 8, 46, 14);

        // act
        var success = type.TryDeserialize(dateTime, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Equal(dateTime, Assert.IsType<DateTime>(deserialized));
    }

    [Fact]
    public void Deserialize_NullableDateTime_To_DateTime_2()
    {
        // arrange
        var type = new LocalDateTimeType();
        DateTime? dateTime = null;

        // act
        var success = type.TryDeserialize(dateTime, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    public void Deserialize_Null_To_Null()
    {
        // arrange
        var type = new LocalDateTimeType();

        // act
        var success = type.TryDeserialize(null, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    public void ParseLiteral_StringValueNode()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();
        var literal = new StringValueNode("2018-06-29T08:46:14");
        var expectedDateTime = new DateTime(2018, 6, 29, 8, 46, 14);

        // act
        var dateTime = (DateTime)localDateTimeType.ParseLiteral(literal)!;

        // assert
        Assert.Equal(expectedDateTime, dateTime);
    }

    [InlineData("en-US")]
    [InlineData("en-AU")]
    [InlineData("en-GB")]
    [InlineData("de-CH")]
    [InlineData("de-de")]
    [Theory]
    public void ParseLiteral_StringValueNode_DifferentCulture(
        string cultureName)
    {
        // arrange
        Thread.CurrentThread.CurrentCulture =
            CultureInfo.GetCultureInfo(cultureName);

        var localDateTimeType = new LocalDateTimeType();
        var literal = new StringValueNode("2018-06-29T08:46:14");
        var expectedDateTime = new DateTime(2018, 6, 29, 8, 46, 14);

        // act
        var dateTime = (DateTime)localDateTimeType.ParseLiteral(literal)!;

        // assert
        Assert.Equal(expectedDateTime, dateTime);
    }

    [Fact]
    public void ParseLiteral_NullValueNode()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();
        var literal = NullValueNode.Default;

        // act
        var value = localDateTimeType.ParseLiteral(literal);

        // assert
        Assert.Null(value);
    }

    [Fact]
    public void ParseValue_DateTime()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();
        var dateTime = new DateTime(2018, 6, 11, 8, 46, 14);
        const string expectedLiteralValue = "2018-06-11T08:46:14";

        // act
        var stringLiteral =
            (StringValueNode)localDateTimeType.ParseValue(dateTime);

        // assert
        Assert.Equal(expectedLiteralValue, stringLiteral.Value);
    }

    [Fact]
    public void ParseValue_Null()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();

        // act
        var literal = localDateTimeType.ParseValue(null);

        // assert
        Assert.Equal(NullValueNode.Default, literal);
    }

    [Fact]
    public void ParseResult_DateTime()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();
        var resultValue = new DateTime(2023, 6, 19, 11, 24, 0, DateTimeKind.Utc);
        const string expectedLiteralValue = "2023-06-19T11:24:00";

        // act
        var literal = localDateTimeType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_DateTimeOffset()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();
        var resultValue = new DateTimeOffset(2023, 6, 19, 11, 24, 0, new TimeSpan(6, 0, 0));
        const string expectedLiteralValue = "2023-06-19T11:24:00";

        // act
        var literal = localDateTimeType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_String()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();
        const string resultValue = "2023-06-19T11:24:00";
        const string expectedLiteralValue = "2023-06-19T11:24:00";

        // act
        var literal = localDateTimeType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_Null()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();

        // act
        var literal = localDateTimeType.ParseResult(null);

        // assert
        Assert.Equal(NullValueNode.Default, literal);
    }

    [Fact]
    public void ParseResult_SerializationException()
    {
        // arrange
        var localDateTimeType = new LocalDateTimeType();
        const int resultValue = 1;

        // act
        var exception = Record.Exception(() => localDateTimeType.ParseResult(resultValue));

        // assert
        Assert.IsType<SerializationException>(exception);
    }

    [Fact]
    public void EnsureLocalDateTimeTypeKindIsCorrect()
    {
        // arrange
        var type = new LocalDateTimeType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    public void LocalDateTimeType_Binds_Only_Explicitly()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType(new LocalDateTimeType())
            .Create();

        // assert
        IType localDateTimeType = schema.QueryType.Fields["localDateTimeField"].Type;
        IType dateTimeType = schema.QueryType.Fields["dateTimeField"].Type;

        Assert.IsType<LocalDateTimeType>(localDateTimeType);
        Assert.IsType<DateTimeType>(dateTimeType);
    }

    [Fact]
    public async Task LocalDateTime_As_Argument_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task LocalDateTime_As_Argument()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .ExecuteRequestAsync(
                """
                {
                    foo {
                        localDateTime(localDateTime: "2017-12-30T11:24:00")
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task LocalDateTime_As_ReturnValue_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task LocalDateTime_As_ReturnValue()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .ExecuteRequestAsync(
                """
                {
                    bar {
                        localDateTime
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    public class Query
    {
        [GraphQLType<LocalDateTimeType>]
        public DateTime? LocalDateTimeField => new();

        public DateTime? DateTimeField => new();
    }

    public class QueryDateTime1
    {
        public Foo Foo => new();
    }

    public class Foo
    {
        [GraphQLType<LocalDateTimeType>]
        public DateTime GetLocalDateTime([GraphQLType<LocalDateTimeType>] DateTime localDateTime)
            => localDateTime;
    }

    public class QueryDateTime2
    {
        public Bar Bar => new();
    }

    public class Bar
    {
        [GraphQLType<LocalDateTimeType>]
        public DateTime GetLocalDateTime() => DateTime.MaxValue;
    }
}
