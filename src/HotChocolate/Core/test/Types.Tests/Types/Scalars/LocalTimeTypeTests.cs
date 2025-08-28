using System.Globalization;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class LocalTimeTypeTests
{
    [Fact]
    public void Serialize_TimeOnly()
    {
        // arrange
        var localTimeType = new LocalTimeType();
        var timeOnly = new TimeOnly(8, 46, 14);
        const string expectedValue = "08:46:14";

        // act
        var serializedValue = (string)localTimeType.Serialize(timeOnly);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_DateTime()
    {
        // arrange
        var localTimeType = new LocalTimeType();
        var dateTime = new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
        const string expectedValue = "08:46:14";

        // act
        var serializedValue = (string)localTimeType.Serialize(dateTime);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_DateTimeOffset()
    {
        // arrange
        var localTimeType = new LocalTimeType();
        var dateTime = new DateTimeOffset(
            new DateTime(2018, 6, 11, 2, 46, 14),
            new TimeSpan(4, 0, 0));
        const string expectedValue = "02:46:14";

        // act
        var serializedValue = (string)localTimeType.Serialize(dateTime);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_Null()
    {
        // arrange
        var localTimeType = new LocalTimeType();

        // act
        var serializedValue = localTimeType.Serialize(null);

        // assert
        Assert.Null(serializedValue);
    }

    [Fact]
    public void Serialize_String_Exception()
    {
        // arrange
        var localTimeType = new LocalTimeType();

        // act
        void Action() => localTimeType.Serialize("foo");

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void Deserialize_IsoString_TimeOnly()
    {
        // arrange
        var localTimeType = new LocalTimeType();
        var time = new TimeOnly(8, 46, 14);

        // act
        var result = (TimeOnly)localTimeType.Deserialize("08:46:14")!;

        // assert
        Assert.Equal(time, result);
    }

    [Fact]
    public void Deserialize_InvalidFormat_To_TimeOnly()
    {
        // arrange
        var type = new LocalTimeType();

        // act
        var success = type.TryDeserialize("08:46:14 pm", out _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void Deserialize_InvalidString_To_TimeOnly()
    {
        // arrange
        var type = new LocalTimeType();

        // act
        var success = type.TryDeserialize("abc", out _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void Deserialize_TimeOnly_To_TimeOnly()
    {
        // arrange
        var type = new LocalTimeType();
        var time = new TimeOnly(8, 46, 14);

        // act
        var success = type.TryDeserialize(time, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Equal(time, deserialized);
    }

    [Fact]
    public void Deserialize_DateTime_To_TimeOnly()
    {
        // arrange
        var type = new LocalTimeType();
        var date = new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);

        // act
        var success = type.TryDeserialize(date, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Equal(TimeOnly.FromDateTime(date),
            Assert.IsType<TimeOnly>(deserialized));
    }

    [Fact]
    public void Deserialize_DateTimeOffset_To_TimeOnly()
    {
        // arrange
        var type = new LocalTimeType();
        var date = new DateTimeOffset(
            new DateTime(2018, 6, 11, 2, 46, 14),
            new TimeSpan(4, 0, 0));

        // act
        var success = type.TryDeserialize(date, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Equal(TimeOnly.FromDateTime(date.DateTime),
            Assert.IsType<TimeOnly>(deserialized));
    }

    [Fact]
    public void Deserialize_NullableTimeOnly_To_TimeOnly()
    {
        // arrange
        var type = new LocalTimeType();
        TimeOnly? time = new TimeOnly(8, 46, 14);

        // act
        var success = type.TryDeserialize(time, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Equal(time, Assert.IsType<TimeOnly>(deserialized));
    }

    [Fact]
    public void Deserialize_NullableTimeOnly_To_TimeOnly_2()
    {
        // arrange
        var type = new LocalTimeType();
        TimeOnly? time = null;

        // act
        var success = type.TryDeserialize(time, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    public void Deserialize_Null_To_Null()
    {
        // arrange
        var type = new LocalTimeType();

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
        var localTimeType = new LocalTimeType();
        var literal = new StringValueNode("08:46:14");
        var expectedTimeOnly = new TimeOnly(8, 46, 14);

        // act
        var timeOnly = (TimeOnly)localTimeType.ParseLiteral(literal)!;

        // assert
        Assert.Equal(expectedTimeOnly, timeOnly);
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

        var localTimeType = new LocalTimeType();
        var literal = new StringValueNode("08:46:14");
        var expectedTimeOnly = new TimeOnly(8, 46, 14);

        // act
        var timeOnly = (TimeOnly)localTimeType.ParseLiteral(literal)!;

        // assert
        Assert.Equal(expectedTimeOnly, timeOnly);
    }

    [Fact]
    public void ParseLiteral_NullValueNode()
    {
        // arrange
        var localTimeType = new LocalTimeType();
        var literal = NullValueNode.Default;

        // act
        var value = localTimeType.ParseLiteral(literal);

        // assert
        Assert.Null(value);
    }

    [Fact]
    public void ParseValue_TimeOnly()
    {
        // arrange
        var localTimeType = new LocalTimeType();
        var timeOnly = new TimeOnly(8, 46, 14);
        const string expectedLiteralValue = "08:46:14";

        // act
        var stringLiteral =
            (StringValueNode)localTimeType.ParseValue(timeOnly);

        // assert
        Assert.Equal(expectedLiteralValue, stringLiteral.Value);
    }

    [Fact]
    public void ParseValue_Null()
    {
        // arrange
        var localTimeType = new LocalTimeType();

        // act
        var literal = localTimeType.ParseValue(null);

        // assert
        Assert.Equal(NullValueNode.Default, literal);
    }

    [Fact]
    public void ParseResult_TimeOnly()
    {
        // arrange
        var localTimeType = new LocalTimeType();
        var resultValue = new TimeOnly(8, 46, 14);
        const string expectedLiteralValue = "08:46:14";

        // act
        var literal = localTimeType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_DateTime()
    {
        // arrange
        var localTimeType = new LocalTimeType();
        var resultValue = new DateTime(2023, 6, 19, 11, 24, 0, DateTimeKind.Utc);
        const string expectedLiteralValue = "11:24:00";

        // act
        var literal = localTimeType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_DateTimeOffset()
    {
        // arrange
        var localTimeType = new LocalTimeType();
        var resultValue = new DateTimeOffset(2023, 6, 19, 11, 24, 0, new TimeSpan(6, 0, 0));
        const string expectedLiteralValue = "11:24:00";

        // act
        var literal = localTimeType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_String()
    {
        // arrange
        var localTimeType = new LocalTimeType();
        const string resultValue = "11:24:00";
        const string expectedLiteralValue = "11:24:00";

        // act
        var literal = localTimeType.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_Null()
    {
        // arrange
        var localTimeType = new LocalTimeType();

        // act
        var literal = localTimeType.ParseResult(null);

        // assert
        Assert.Equal(NullValueNode.Default, literal);
    }

    [Fact]
    public void ParseResult_SerializationException()
    {
        // arrange
        var localTimeType = new LocalTimeType();
        const int resultValue = 1;

        // act
        var exception = Record.Exception(() => localTimeType.ParseResult(resultValue));

        // assert
        Assert.IsType<SerializationException>(exception);
    }

    [Fact]
    public void EnsureLocalTimeTypeKindIsCorrect()
    {
        // arrange
        var type = new LocalTimeType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    public void LocalTimeType_Binds_Only_Explicitly()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType(new LocalTimeType())
            .Create();

        // assert
        IType localTimeType = schema.QueryType.Fields["timeField"].Type;
        IType dateTimeType = schema.QueryType.Fields["dateTimeField"].Type;

        Assert.IsType<LocalTimeType>(localTimeType);
        Assert.IsType<DateTimeType>(dateTimeType);
    }

    [Fact]
    public async Task TimeOnly_As_Argument_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task TimeOnly_As_Argument()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .ExecuteRequestAsync(
                """
                {
                    foo {
                        time(time: "11:22:00")
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task TimeOnly_As_ReturnValue_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task TimeOnly_As_ReturnValue()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .ExecuteRequestAsync(
                """
                {
                    bar {
                        time
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    public class Query
    {
        [GraphQLType(typeof(LocalTimeType))]
        public TimeOnly? TimeField => new();

        public DateTime? DateTimeField => new();
    }

    public class QueryDateTime1
    {
        public Foo Foo => new();
    }

    public class Foo
    {
        public TimeOnly GetTime(TimeOnly time) => time;
    }

    public class QueryDateTime2
    {
        public Bar Bar => new();
    }

    public class Bar
    {
        public TimeOnly GetTime() => TimeOnly.MaxValue;
    }
}
