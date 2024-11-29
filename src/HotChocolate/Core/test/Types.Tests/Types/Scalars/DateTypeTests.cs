using System.Globalization;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class DateTypeTests
{
    [Fact]
    public void Serialize_DateOnly()
    {
        // arrange
        var dateType = new DateType();
        var dateOnly = new DateOnly(2018, 6, 11);
        var expectedValue = "2018-06-11";

        // act
        var serializedValue = (string)dateType.Serialize(dateOnly);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_DateTime()
    {
        // arrange
        var dateType = new DateType();
        var dateTime = new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
        var expectedValue = "2018-06-11";

        // act
        var serializedValue = (string)dateType.Serialize(dateTime);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_DateTimeOffset()
    {
        // arrange
        var dateType = new DateType();
        var dateTime = new DateTimeOffset(
            new DateTime(2018, 6, 11, 2, 46, 14),
            new TimeSpan(4, 0, 0));
        var expectedValue = "2018-06-10";

        // act
        var serializedValue = (string)dateType.Serialize(dateTime);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_Null()
    {
        // arrange
        var dateType = new DateType();

        // act
        var serializedValue = dateType.Serialize(null);

        // assert
        Assert.Null(serializedValue);
    }

    [Fact]
    public void Serialize_String_Exception()
    {
        // arrange
        var dateType = new DateType();

        // act
        void Action() => dateType.Serialize("foo");

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void Deserialize_IsoString_DateOnly()
    {
        // arrange
        var dateType = new DateType();
        var date = new DateOnly(2018, 6, 11);

        // act
        var result = (DateOnly)dateType.Deserialize("2018-06-11")!;

        // assert
        Assert.Equal(date, result);
    }

    [Fact]
    public void Deserialize_InvalidFormat_To_DateOnly()
    {
        // arrange
        var type = new DateType();

        // act
        var success = type.TryDeserialize("2018/06/11", out _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void Deserialize_InvalidString_To_DateOnly()
    {
        // arrange
        var type = new DateType();

        // act
        var success = type.TryDeserialize("abc", out _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void Deserialize_DateOnly_To_DateOnly()
    {
        // arrange
        var type = new DateType();
        var date = new DateOnly(2018, 6, 11);

        // act
        var success = type.TryDeserialize(date, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Equal(date, deserialized);
    }

    [Fact]
    public void Deserialize_DateTime_To_DateOnly()
    {
        // arrange
        var type = new DateType();
        var date = new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);

        // act
        var success = type.TryDeserialize(date, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Equal(DateOnly.FromDateTime(date),
            Assert.IsType<DateOnly>(deserialized));
    }

    [Fact]
    public void Deserialize_DateTimeOffset_To_DateOnly()
    {
        // arrange
        var type = new DateType();
        var date = new DateTimeOffset(
            new DateTime(2018, 6, 11, 2, 46, 14),
            new TimeSpan(4, 0, 0));

        // act
        var success = type.TryDeserialize(date, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Equal(DateOnly.FromDateTime(date.UtcDateTime),
            Assert.IsType<DateOnly>(deserialized));
    }

    [Fact]
    public void Deserialize_NullableDateOnly_To_DateOnly()
    {
        // arrange
        var type = new DateType();
        DateOnly? date = new DateOnly(2018, 6, 11);

        // act
        var success = type.TryDeserialize(date, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Equal(date, Assert.IsType<DateOnly>(deserialized));
    }

    [Fact]
    public void Deserialize_NullableDateOnly_To_DateOnly_2()
    {
        // arrange
        var type = new DateType();
        DateOnly? date = null;

        // act
        var success = type.TryDeserialize(date, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    public void Deserialize_Null_To_Null()
    {
        // arrange
        var type = new DateType();

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
        var dateType = new DateType();
        var literal = new StringValueNode("2018-06-29");
        var expectedDateTime = new DateOnly(2018, 6, 29);

        // act
        var dateTime = (DateOnly)dateType.ParseLiteral(literal)!;

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

        var dateType = new DateType();
        var literal = new StringValueNode("2018-06-29");
        var expectedDateTime = new DateOnly(2018, 6, 29);

        // act
        var dateTime = (DateOnly)dateType.ParseLiteral(literal)!;

        // assert
        Assert.Equal(expectedDateTime, dateTime);
    }

    [Fact]
    public void ParseLiteral_NullValueNode()
    {
        // arrange
        var dateType = new DateType();
        var literal = NullValueNode.Default;

        // act
        var value = dateType.ParseLiteral(literal);

        // assert
        Assert.Null(value);
    }

    [Fact]
    public void ParseValue_DateOnly()
    {
        // arrange
        var dateType = new DateType();
        var dateOnly = new DateOnly(2018, 6, 11);
        var expectedLiteralValue = "2018-06-11";

        // act
        var stringLiteral =
            (StringValueNode)dateType.ParseValue(dateOnly);

        // assert
        Assert.Equal(expectedLiteralValue, stringLiteral.Value);
    }

    [Fact]
    public void ParseValue_Null()
    {
        // arrange
        var dateType = new DateType();

        // act
        var literal = dateType.ParseValue(null);

        // assert
        Assert.Equal(NullValueNode.Default, literal);
    }

    [Fact]
    public void ParseResult_DateOnly()
    {
        // arrange
        var dateType = new DateType();
        var resultValue = new DateOnly(2023, 6, 19);
        var expectedLiteralValue = "2023-06-19";

        // act
        var literal = dateType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_DateTime()
    {
        // arrange
        var dateType = new DateType();
        var resultValue = new DateTime(2023, 6, 19, 11, 24, 0, DateTimeKind.Utc);
        var expectedLiteralValue = "2023-06-19";

        // act
        var literal = dateType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_DateTimeOffset()
    {
        // arrange
        var dateType = new DateType();
        var resultValue = new DateTimeOffset(2023, 6, 19, 11, 24, 0, new TimeSpan(6, 0, 0));
        var expectedLiteralValue = "2023-06-19";

        // act
        var literal = dateType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_String()
    {
        // arrange
        var dateType = new DateType();
        var resultValue = "2023-06-19";
        var expectedLiteralValue = "2023-06-19";

        // act
        var literal = dateType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_Null()
    {
        // arrange
        var dateType = new DateType();

        // act
        var literal = dateType.ParseResult(null);

        // assert
        Assert.Equal(NullValueNode.Default, literal);
    }

    [Fact]
    public void ParseResult_SerializationException()
    {
        // arrange
        var dateType = new DateType();
        var resultValue = 1;

        // act
        var exception = Record.Exception(() => dateType.ParseResult(resultValue));

        // assert
        Assert.IsType<SerializationException>(exception);
    }

    [Fact]
    public void EnsureDateTypeKindIsCorrect()
    {
        // arrange
        var type = new DateType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    public void DateType_Binds_Only_Explicitly()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType(new DateType())
            .Create();

        // assert
        IType dateType = schema.QueryType.Fields["dateField"].Type;
        IType dateTimeType = schema.QueryType.Fields["dateTimeField"].Type;

        Assert.IsType<DateType>(dateType);
        Assert.IsType<DateTimeType>(dateTimeType);
    }

    [Fact]
    public async Task DateOnly_And_TimeOnly_As_Argument_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DateOnly_And_TimeOnly_As_Argument()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .AddType(() => new TimeSpanType(TimeSpanFormat.DotNet))
            .ExecuteRequestAsync(
                @"{
                        foo {
                            time(time: ""11:22"")
                            date(date: ""2017-12-30"")
                        }
                    }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DateOnly_And_TimeOnly_As_ReturnValue_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DateOnly_And_TimeOnly_As_ReturnValue()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .AddType(() => new TimeSpanType(TimeSpanFormat.DotNet))
            .ExecuteRequestAsync(
                @"{
                        bar {
                            time
                            date
                        }
                    }")
            .MatchSnapshotAsync();
    }

    public class Query
    {
        [GraphQLType(typeof(DateType))]
        public DateTime? DateField => DateTime.UtcNow;

        public DateTime? DateTimeField => DateTime.UtcNow;
    }

    public class QueryDateTime1
    {
        public Foo Foo => new();
    }

    public class Foo
    {
        public TimeSpan GetTime(TimeOnly time) => time.ToTimeSpan();

        public DateTime GetDate(DateOnly date)
            => date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
    }

    public class QueryDateTime2
    {
        public Bar Bar => new();
    }

    public class Bar
    {
        public TimeOnly GetTime() => TimeOnly.MaxValue;

        public DateOnly GetDate() => DateOnly.MaxValue;
    }
}
