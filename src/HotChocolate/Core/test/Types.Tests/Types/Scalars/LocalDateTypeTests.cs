using System.Globalization;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class LocalDateTypeTests
{
    [Fact]
    public void Serialize_DateOnly()
    {
        // arrange
        var localDateType = new LocalDateType();
        var dateOnly = new DateOnly(2018, 6, 11);
        const string expectedValue = "2018-06-11";

        // act
        var serializedValue = (string)localDateType.Serialize(dateOnly);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_DateTime()
    {
        // arrange
        var localDateType = new LocalDateType();
        var dateTime = new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
        const string expectedValue = "2018-06-11";

        // act
        var serializedValue = (string)localDateType.Serialize(dateTime);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_DateTimeOffset()
    {
        // arrange
        var localDateType = new LocalDateType();
        var dateTime = new DateTimeOffset(
            new DateTime(2018, 6, 11, 2, 46, 14),
            new TimeSpan(4, 0, 0));
        const string expectedValue = "2018-06-11";

        // act
        var serializedValue = (string)localDateType.Serialize(dateTime);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_Null()
    {
        // arrange
        var localDateType = new LocalDateType();

        // act
        var serializedValue = localDateType.Serialize(null);

        // assert
        Assert.Null(serializedValue);
    }

    [Fact]
    public void Serialize_String_Exception()
    {
        // arrange
        var localDateType = new LocalDateType();

        // act
        void Action() => localDateType.Serialize("foo");

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void Deserialize_IsoString_DateOnly()
    {
        // arrange
        var localDateType = new LocalDateType();
        var date = new DateOnly(2018, 6, 11);

        // act
        var result = (DateOnly)localDateType.Deserialize("2018-06-11")!;

        // assert
        Assert.Equal(date, result);
    }

    [Fact]
    public void Deserialize_InvalidFormat_To_DateOnly()
    {
        // arrange
        var type = new LocalDateType();

        // act
        var success = type.TryDeserialize("2018/06/11", out _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void Deserialize_InvalidString_To_DateOnly()
    {
        // arrange
        var type = new LocalDateType();

        // act
        var success = type.TryDeserialize("abc", out _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void Deserialize_DateOnly_To_DateOnly()
    {
        // arrange
        var type = new LocalDateType();
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
        var type = new LocalDateType();
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
        var type = new LocalDateType();
        var date = new DateTimeOffset(
            new DateTime(2018, 6, 11, 2, 46, 14),
            new TimeSpan(4, 0, 0));

        // act
        var success = type.TryDeserialize(date, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Equal(DateOnly.FromDateTime(date.DateTime),
            Assert.IsType<DateOnly>(deserialized));
    }

    [Fact]
    public void Deserialize_NullableDateOnly_To_DateOnly()
    {
        // arrange
        var type = new LocalDateType();
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
        var type = new LocalDateType();
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
        var type = new LocalDateType();

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
        var localDateType = new LocalDateType();
        var literal = new StringValueNode("2018-06-29");
        var expectedDateOnly = new DateOnly(2018, 6, 29);

        // act
        var dateOnly = (DateOnly)localDateType.ParseLiteral(literal)!;

        // assert
        Assert.Equal(expectedDateOnly, dateOnly);
    }

    [Theory]
    [MemberData(nameof(ValidLocalDateScalarStrings))]
    public void ParseLiteral_StringValueNode_Valid(string dateTime, DateOnly result)
    {
        // arrange
        var localDateType = new LocalDateType();
        var literal = new StringValueNode(dateTime);

        // act
        var dateTimeOffset = (DateOnly?)localDateType.ParseLiteral(literal);

        // assert
        Assert.Equal(result, dateTimeOffset);
    }

    [Theory]
    [MemberData(nameof(InvalidLocalDateScalarStrings))]
    public void ParseLiteral_StringValueNode_Invalid(string dateTime)
    {
        // arrange
        var localDateType = new LocalDateType();
        var literal = new StringValueNode(dateTime);

        // act
        void Act()
        {
            localDateType.ParseLiteral(literal);
        }

        // assert
        Assert.Equal(
            "LocalDate cannot parse the given literal of type `StringValueNode`.",
            Assert.Throws<SerializationException>(Act).Message);
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

        var localDateType = new LocalDateType();
        var literal = new StringValueNode("2018-06-29");
        var expectedDateOnly = new DateOnly(2018, 6, 29);

        // act
        var dateOnly = (DateOnly)localDateType.ParseLiteral(literal)!;

        // assert
        Assert.Equal(expectedDateOnly, dateOnly);
    }

    [Fact]
    public void ParseLiteral_NullValueNode()
    {
        // arrange
        var localDateType = new LocalDateType();
        var literal = NullValueNode.Default;

        // act
        var value = localDateType.ParseLiteral(literal);

        // assert
        Assert.Null(value);
    }

    [Fact]
    public void ParseValue_DateOnly()
    {
        // arrange
        var localDateType = new LocalDateType();
        var dateOnly = new DateOnly(2018, 6, 11);
        const string expectedLiteralValue = "2018-06-11";

        // act
        var stringLiteral =
            (StringValueNode)localDateType.ParseValue(dateOnly);

        // assert
        Assert.Equal(expectedLiteralValue, stringLiteral.Value);
    }

    [Fact]
    public void ParseValue_Null()
    {
        // arrange
        var localDateType = new LocalDateType();

        // act
        var literal = localDateType.ParseValue(null);

        // assert
        Assert.Equal(NullValueNode.Default, literal);
    }

    [Fact]
    public void ParseResult_DateOnly()
    {
        // arrange
        var localDateType = new LocalDateType();
        var resultValue = new DateOnly(2023, 6, 19);
        const string expectedLiteralValue = "2023-06-19";

        // act
        var literal = localDateType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_DateTime()
    {
        // arrange
        var localDateType = new LocalDateType();
        var resultValue = new DateTime(2023, 6, 19, 11, 24, 0, DateTimeKind.Utc);
        const string expectedLiteralValue = "2023-06-19";

        // act
        var literal = localDateType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_DateTimeOffset()
    {
        // arrange
        var localDateType = new LocalDateType();
        var resultValue = new DateTimeOffset(2023, 6, 19, 11, 24, 0, new TimeSpan(6, 0, 0));
        const string expectedLiteralValue = "2023-06-19";

        // act
        var literal = localDateType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_String()
    {
        // arrange
        var localDateType = new LocalDateType();
        const string resultValue = "2023-06-19";
        const string expectedLiteralValue = "2023-06-19";

        // act
        var literal = localDateType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_Null()
    {
        // arrange
        var localDateType = new LocalDateType();

        // act
        var literal = localDateType.ParseResult(null);

        // assert
        Assert.Equal(NullValueNode.Default, literal);
    }

    [Fact]
    public void ParseResult_SerializationException()
    {
        // arrange
        var localDateType = new LocalDateType();
        const int resultValue = 1;

        // act
        var exception = Record.Exception(() => localDateType.ParseResult(resultValue));

        // assert
        Assert.IsType<SerializationException>(exception);
    }

    [Fact]
    public void EnsureLocalDateTypeKindIsCorrect()
    {
        // arrange
        var type = new LocalDateType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    public void LocalDateType_Binds_Only_Explicitly()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType(new LocalDateType())
            .Create();

        // assert
        IType localDateType = schema.QueryType.Fields["dateField"].Type;
        IType dateTimeType = schema.QueryType.Fields["dateTimeField"].Type;

        Assert.IsType<LocalDateType>(localDateType);
        Assert.IsType<DateTimeType>(dateTimeType);
    }

    [Fact]
    public async Task DateOnly_As_Argument_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DateOnly_As_Argument()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .ExecuteRequestAsync(
                """
                {
                    foo {
                        date(date: "2017-12-30")
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DateOnly_As_ReturnValue_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DateOnly_As_ReturnValue()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .ExecuteRequestAsync(
                """
                {
                    bar {
                        date
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    public class Query
    {
        [GraphQLType(typeof(LocalDateType))]
        public DateOnly? DateField => new();

        public DateTime? DateTimeField => new();
    }

    public class QueryDateTime1
    {
        public Foo Foo => new();
    }

    public class Foo
    {
        public DateOnly GetDate(DateOnly date) => date;
    }

    public class QueryDateTime2
    {
        public Bar Bar => new();
    }

    public class Bar
    {
        public DateOnly GetDate() => DateOnly.MaxValue;
    }

    public static TheoryData<string, DateOnly> ValidLocalDateScalarStrings()
    {
        return new TheoryData<string, DateOnly>
        {
            // https://scalars.graphql.org/andimarek/local-date.html#sec-Overview
            {
                "1983-10-20",
                new(1983, 10, 20)
            },
            {
                "2023-04-01",
                new(2023, 4, 1)
            }
        };
    }

    public static TheoryData<string> InvalidLocalDateScalarStrings()
    {
        return
        [
            // https://scalars.graphql.org/andimarek/local-date.html#sec-Overview
            // There isn't a 13th month in a year.
            "2011-13-10"
        ];
    }
}
