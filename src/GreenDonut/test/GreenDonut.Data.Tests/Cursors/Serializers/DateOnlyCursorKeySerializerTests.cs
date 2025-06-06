namespace GreenDonut.Data.Cursors.Serializers;

public class DateOnlyCursorKeySerializerTests
{
    private static readonly DateOnlyCursorKeySerializer s_serializer = new();

    [Theory]
    [MemberData(nameof(Data))]
    public void Format(DateOnly dateOnly, byte[] result)
    {
        // arrange
        Span<byte> buffer = stackalloc byte[8];

        // act
        var success = s_serializer.TryFormat(dateOnly, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(result, buffer);
        Assert.Equal(8, written);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Parse(DateOnly result, byte[] formattedKey)
    {
        // arrange & act
        var dateOnly = (DateOnly)s_serializer.Parse(formattedKey);

        // assert
        Assert.Equal(result, dateOnly);
    }

    public static TheoryData<DateOnly, byte[]> Data()
    {
        return new TheoryData<DateOnly, byte[]>
        {
            {
                DateOnly.MinValue,
                "00010101"u8.ToArray()
            },
            {
                DateOnly.FromDateTime(DateTime.UnixEpoch),
                "19700101"u8.ToArray()
            },
            {
                DateOnly.MaxValue,
                "99991231"u8.ToArray()
            }
        };
    }
}
