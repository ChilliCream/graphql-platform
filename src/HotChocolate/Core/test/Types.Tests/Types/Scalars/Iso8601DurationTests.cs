namespace HotChocolate.Types;

public class Iso8601DurationTests
{
    public static IEnumerable<object[]> TryParseTests => new List<object[]>
    {
        new object[] { "-P1D", TimeSpan.FromDays(-1), },
        new object[] { "PT0.0000001S", TimeSpan.FromMilliseconds(1) / 1000 / 10, },
        new object[] { "-PT0.0000001S", TimeSpan.FromMilliseconds(-1) / 1000 / 10, },
    };

    [Theory]
    [MemberData(nameof(TryParseTests))]
    public void TryParse(string duration, TimeSpan? expected)
    {
        // act
        var result = Iso8601Duration.TryParse(duration, out var actual);

        // assert
        Assert.True(result);
        Assert.Equal(expected, actual);
    }
}
