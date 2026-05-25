using System.Diagnostics;

namespace Mocha.Tests;

public class TraceparentHelperTests
{
    [Fact]
    public void FormatTraceparent_Activity_Returns_Correct_Format()
    {
        // arrange
        using var source = new ActivitySource("test");
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("op");
        Assert.NotNull(activity);

        // act
        var result = TraceparentHelper.FormatTraceparent(activity);

        // assert
        Assert.NotNull(result);
        Assert.Equal(55, result.Length);

        var parts = result.Split('-');
        Assert.Equal(4, parts.Length);
        Assert.Equal("00", parts[0]);
        Assert.Equal(32, parts[1].Length);
        Assert.Equal(16, parts[2].Length);
        Assert.Equal(2, parts[3].Length);
    }

    [Fact]
    public void FormatTraceparent_Activity_Matches_TraceId_And_SpanId()
    {
        // arrange
        using var source = new ActivitySource("test");
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("op");
        Assert.NotNull(activity);

        // act
        var result = TraceparentHelper.FormatTraceparent(activity);

        // assert
        Assert.NotNull(result);
        var expected = $"00-{activity.TraceId.ToHexString()}-{activity.SpanId.ToHexString()}-01";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatTraceparent_Activity_Returns_Null_When_Default_TraceId()
    {
        // arrange - activity not started, has default trace/span IDs
        var activity = new Activity("not-started");

        // act
        var result = TraceparentHelper.FormatTraceparent(activity);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void FormatTraceparent_Recorded_Flag_Sets_01()
    {
        // arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();

        // act
        var result = TraceparentHelper.FormatTraceparent(
            traceId, spanId, ActivityTraceFlags.Recorded);

        // assert
        Assert.EndsWith("-01", result);
    }

    [Fact]
    public void FormatTraceparent_None_Flag_Sets_00()
    {
        // arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();

        // act
        var result = TraceparentHelper.FormatTraceparent(
            traceId, spanId, ActivityTraceFlags.None);

        // assert
        Assert.EndsWith("-00", result);
    }

    [Fact]
    public void FormatTraceparent_Ids_Roundtrip_Correctly()
    {
        // arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();

        // act
        var result = TraceparentHelper.FormatTraceparent(
            traceId, spanId, ActivityTraceFlags.Recorded);

        // assert
        var parts = result.Split('-');
        Assert.Equal(traceId.ToHexString(), parts[1]);
        Assert.Equal(spanId.ToHexString(), parts[2]);
    }

    [Fact]
    public void FormatTraceparent_Always_Returns_Lowercase_Hex()
    {
        // arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();

        // act
        var result = TraceparentHelper.FormatTraceparent(
            traceId, spanId, ActivityTraceFlags.Recorded);

        // assert - all hex chars should be lowercase
        foreach (var c in result)
        {
            if (char.IsLetter(c))
            {
                Assert.True(char.IsLower(c), $"Expected lowercase but found '{c}' in: {result}");
            }
        }
    }

    [Fact]
    public void FormatTraceparent_Is_Parseable_By_ActivityContext()
    {
        // arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();

        // act
        var result = TraceparentHelper.FormatTraceparent(
            traceId, spanId, ActivityTraceFlags.Recorded);

        // assert - ActivityContext.TryParse should accept our output
        Assert.True(
            ActivityContext.TryParse(result, null, out var parsed),
            $"ActivityContext.TryParse failed for: {result}");
        Assert.Equal(traceId, parsed.TraceId);
        Assert.Equal(spanId, parsed.SpanId);
        Assert.Equal(ActivityTraceFlags.Recorded, parsed.TraceFlags);
    }

    [Fact]
    public void FormatTraceparent_Length_Is_Always_55()
    {
        // run multiple times to test with different random IDs
        for (var i = 0; i < 100; i++)
        {
            var traceId = ActivityTraceId.CreateRandom();
            var spanId = ActivitySpanId.CreateRandom();

            var result = TraceparentHelper.FormatTraceparent(
                traceId, spanId, ActivityTraceFlags.None);

            Assert.Equal(55, result.Length);
        }
    }
}
