using HotChocolate.Pagination.Serialization;

namespace HotChocolate.Data.Serialization;

public class DateTimeCursorKeySerializerTests
{
    private static readonly DateTimeCursorKeySerializer Serializer = new();

    [Theory]
    [MemberData(nameof(Data))]
    public void Format(DateTime dateTime, byte[] result)
    {
        // arrange
        Span<byte> buffer = stackalloc byte[23];

        // act
        var success = Serializer.TryFormat(dateTime, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(result, buffer);
        Assert.Equal(23, written);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Parse(DateTime result, byte[] formattedKey)
    {
        // arrange & act
        var dateTime = (DateTime)Serializer.Parse(formattedKey);

        // assert
        Assert.Equal(result, dateTime);
        Assert.Equal(result.Kind, dateTime.Kind);
    }

    public static TheoryData<DateTime, byte[]> Data()
    {
        return new TheoryData<DateTime, byte[]>
        {
            // kind: unspecified
            {
                DateTime.MinValue,
                "000101010000000000000#0"u8.ToArray()
            },
            {
                DateTime.MaxValue,
                "999912312359599999999#0"u8.ToArray()
            },
            // kind: UTC
            {
                new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Utc),
                "000101010000000000000#1"u8.ToArray()
            },
            {
                DateTime.UnixEpoch,
                "197001010000000000000#1"u8.ToArray()
            },
            {
                new DateTime(DateTime.MaxValue.Ticks, DateTimeKind.Utc),
                "999912312359599999999#1"u8.ToArray()
            },
            // kind: local
            {
                new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Local),
                "000101010000000000000#2"u8.ToArray()
            },
            {
                new DateTime(DateTime.MaxValue.Ticks, DateTimeKind.Local),
                "999912312359599999999#2"u8.ToArray()
            }
        };
    }
}
