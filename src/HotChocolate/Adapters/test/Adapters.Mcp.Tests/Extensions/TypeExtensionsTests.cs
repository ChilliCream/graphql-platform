using System.Text.RegularExpressions;
using HotChocolate.Types;
using Json.Schema;

namespace HotChocolate.Adapters.Mcp.Extensions;

public sealed class TypeExtensionsTests
{
    [Theory]
    [InlineData(typeof(Base64StringType), "dmFsdWU=")]
    // A DateTime with UTC offset (+00:00).
    [InlineData(typeof(DateTimeType), "2011-08-30T13:22:53.108Z")]
    // A DateTime with +00:00 which is the same as UTC.
    [InlineData(typeof(DateTimeType), "2011-08-30T13:22:53.108+00:00")]
    // The z and t may be lower case.
    [InlineData(typeof(DateTimeType), "2011-08-30t13:22:53.108z")]
    // A DateTime with -3h offset.
    [InlineData(typeof(DateTimeType), "2011-08-30T13:22:53.108-03:00")]
    // A DateTime with +3h 30min offset.
    [InlineData(typeof(DateTimeType), "2011-08-30T13:22:53.108+03:30")]
    // A DateTime with 7 fractional digits.
    [InlineData(typeof(DateTimeType), "2011-08-30T13:22:53.1230000+03:30")]
    // A DateTime with no fractional seconds.
    [InlineData(typeof(DateTimeType), "2011-08-30T13:22:53+03:30")]
    [InlineData(typeof(LocalDateTimeType), "2011-08-30T13:22:53")]
    [InlineData(typeof(LocalTimeType), "13:22:53")]
    [InlineData(typeof(TimeSpanType), "P18M")]
    [InlineData(typeof(TimeSpanType), "PT1H30M")]
    [InlineData(typeof(TimeSpanType), "P1Y2M3DT4H5M6S")]
    [InlineData(typeof(TimeSpanType), "PT4.567S")]
    public void ToJsonSchemaBuilder_ValidValues_MatchPattern(Type type, string value)
    {
        // arrange
        var instance = (IType)Activator.CreateInstance(type)!;

        // act
        var jsonSchema = instance.ToJsonSchemaBuilder().Build();
        var regex = new Regex(jsonSchema.GetPatternValue()!);

        // assert
        Assert.Matches(regex, value);
    }

    [Theory]
    [InlineData(typeof(Base64StringType), "invalidBase64")]
    // The minutes of the offset are missing.
    [InlineData(typeof(DateTimeType), "2011-08-30T13:22:53.108-03")]
    // No offset provided.
    [InlineData(typeof(DateTimeType), "2011-08-30T13:22:53.108")]
    // No time provided.
    [InlineData(typeof(DateTimeType), "2011-08-30")]
    // Seconds are not allowed for the offset.
    [InlineData(typeof(DateTimeType), "2011-08-30T13:22:53.108+03:30:15")]
    // A DateTime with 10 fractional digits.
    [InlineData(typeof(DateTimeType), "2011-08-30T13:22:53.1234567890+03:30")]
    [InlineData(typeof(LocalDateTimeType), "2018/06/11T08:46:14 pm")]
    [InlineData(typeof(LocalDateTimeType), "abc")]
    [InlineData(typeof(LocalTimeType), "08:46:14 pm")]
    [InlineData(typeof(LocalTimeType), "abc")]
    [InlineData(typeof(TimeSpanType), "bad")]
    public void ToJsonSchemaBuilder_InvalidValues_DoNotMatchPattern(Type type, string value)
    {
        // arrange
        var instance = (IType)Activator.CreateInstance(type)!;

        // act
        var jsonSchema = instance.ToJsonSchemaBuilder().Build();
        var regex = new Regex(jsonSchema.GetPatternValue()!);

        // assert
        Assert.DoesNotMatch(regex, value);
    }

    [Theory]
    [InlineData("-10675199.02:48:05.4775808")] // TimeSpan.MinValue.
    [InlineData("10675199.02:48:05.4775807")]  // TimeSpan.MaxValue.
    public void ToJsonSchemaBuilder_TimeSpanTypeDotNetValidValues_MatchPattern(string value)
    {
        // arrange
        var timeSpanType = new TimeSpanType(TimeSpanFormat.DotNet);

        // act
        var jsonSchema = timeSpanType.ToJsonSchemaBuilder().Build();
        var regex = new Regex(jsonSchema.GetPatternValue()!);

        // assert
        Assert.Matches(regex, value);
    }

    [Theory]
    [InlineData("bad")]       // Invalid format.
    [InlineData("+01:30:00")] // A leading plus sign is not allowed.
    public void ToJsonSchemaBuilder_TimeSpanTypeDotNetInvalidValues_DoNotMatchPattern(string value)
    {
        // arrange
        var timeSpanType = new TimeSpanType(TimeSpanFormat.DotNet);

        // act
        var jsonSchema = timeSpanType.ToJsonSchemaBuilder().Build();
        var regex = new Regex(jsonSchema.GetPatternValue()!);

        // assert
        Assert.DoesNotMatch(regex, value);
    }
}
