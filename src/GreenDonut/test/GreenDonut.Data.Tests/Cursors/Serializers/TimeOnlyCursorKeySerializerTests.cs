namespace GreenDonut.Data.Cursors.Serializers;

public class TimeOnlyCursorKeySerializerTests
{
    private static readonly TimeOnlyCursorKeySerializer s_serializer = new();

    [Theory]
    [MemberData(nameof(Data))]
    public void Format(TimeOnly timeOnly, byte[] result)
    {
        // arrange
        Span<byte> buffer = stackalloc byte[13];

        // act
        var success = s_serializer.TryFormat(timeOnly, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(result, buffer);
        Assert.Equal(13, written);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Parse(TimeOnly result, byte[] formattedKey)
    {
        // arrange & act
        var timeOnly = (TimeOnly)s_serializer.Parse(formattedKey);

        // assert
        Assert.Equal(result, timeOnly);
    }

    public static TheoryData<TimeOnly, byte[]> Data()
    {
        return new TheoryData<TimeOnly, byte[]>
        {
            {
                TimeOnly.MinValue,
                "0000000000000"u8.ToArray()
            },
            {
                TimeOnly.MaxValue,
                "2359599999999"u8.ToArray()
            }
        };
    }
}
