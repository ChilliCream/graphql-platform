using System.Globalization;
using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class LocalTimeTypeIntegrationTests
{
    [Fact]
    public void CoerceInputLiteral()
    {
        var type = new LocalTimeType();
        var inputValue = new StringValueNode("12:42:13.031011234");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(
            LocalTime.FromHourMinuteSecondMillisecondTick(12, 42, 13, 31, 100).PlusNanoseconds(1234),
            Assert.IsType<LocalTime>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Value_Throws()
    {
        var type = new LocalTimeType();
        var valueLiteral = new StringValueNode("12:42");
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputValue()
    {
        var type = new LocalTimeType();
        var inputValue = ParseInputValue("\"12:42:13.031011234\"");
        var runtimeValue = type.CoerceInputValue(inputValue, null!);
        Assert.Equal(
            LocalTime.FromHourMinuteSecondMillisecondTick(12, 42, 13, 31, 100).PlusNanoseconds(1234),
            Assert.IsType<LocalTime>(runtimeValue));
    }

    [Fact]
    public void CoerceInputValue_Invalid_Value_Throws()
    {
        var type = new LocalTimeType();
        var inputValue = ParseInputValue("\"12:42\"");
        Action error = () => type.CoerceInputValue(inputValue, null!);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        var type = new LocalTimeType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(LocalTime.FromHourMinuteSecondMillisecondTick(12, 42, 13, 31, 100).PlusNanoseconds(1234), resultValue);
        Assert.Equal("12:42:13.031011234", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Value_Throws()
    {
        var type = new LocalTimeType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue("12:42:13.031011234", resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        var type = new LocalTimeType();
        var valueLiteral = type.ValueToLiteral(LocalTime.FromHourMinuteSecondMillisecondTick(12, 42, 13, 31, 100).PlusNanoseconds(1234));
        Assert.Equal("\"12:42:13.031011234\"", valueLiteral.ToString());
    }

    [Fact]
    public void ValueToLiteral_Invalid_Value_Throws()
    {
        var type = new LocalTimeType();
        Action error = () => type.ValueToLiteral("12:42:13.031011234");
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void PatternEmptyThrowSchemaException()
    {
        static object Call() => new LocalTimeType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void LocalTimeType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var localTimeType = new LocalTimeType(
            LocalTimePattern.GeneralIso,
            LocalTimePattern.ExtendedIso);

        localTimeType.Description.MatchInlineSnapshot(
            """
            LocalTime represents a time of day, with no reference to a particular calendar, time zone, or date.

            Allowed patterns:
            - `hh:mm:ss`
            - `hh:mm:ss.sssssssss`

            Examples:
            - `20:00:00`
            - `20:00:00.999`
            """);
    }

    [Fact]
    public void LocalTimeType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var localTimeType = new LocalTimeType(
            LocalTimePattern.Create("mm", CultureInfo.InvariantCulture));

        localTimeType.Description.MatchInlineSnapshot(
            "LocalTime represents a time of day, with no reference to a particular calendar, time zone, or date.");
    }

    [Fact]
    public async Task Ensure_Schema_First_Can_Override_Type()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                """
                type Query {
                    foo: LocalTime
                }

                scalar LocalTime
                """)
            .AddType<LocalTimeType>()
            .UseField(next => next)
            .BuildRequestExecutorAsync();

        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task Ensure_Schema_First_Can_Override_Type_2()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                """
                type Query {
                    foo: LocalTime
                }

                scalar LocalTime
                """)
            .BindScalarType<LocalTimeType>("LocalTime")
            .UseField(next => next)
            .BuildRequestExecutorAsync();

        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task Ensure_Schema_First_Override_Is_Lazy()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                """
                type Query {
                    foo: String
                }
                """)
            .BindScalarType<LocalTimeType>("LocalTime")
            .UseField(next => next)
            .BuildRequestExecutorAsync();

        executor.Schema.MatchSnapshot();
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
