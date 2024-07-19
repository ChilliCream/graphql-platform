using HotChocolate.Pagination.Serialization;

namespace HotChocolate.Data.Serialization;

public class DateTimeOffsetCursorKeySerializerTests
{
    private static readonly DateTimeOffsetCursorKeySerializer Serializer = new();

    [Theory]
    [MemberData(nameof(Data))]
    public void Format(DateTimeOffset dateTimeOffset, byte[] result)
    {
        // arrange
        Span<byte> buffer = stackalloc byte[26];

        // act
        var success = Serializer.TryFormat(dateTimeOffset, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(result, buffer);
        Assert.Equal(26, written);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Parse(DateTimeOffset result, byte[] formattedKey)
    {
        // arrange & act
        var dateTimeOffset = (DateTimeOffset)Serializer.Parse(formattedKey);

        // assert
        Assert.Equal(result, dateTimeOffset);
    }

    public static TheoryData<DateTimeOffset, byte[]> Data()
    {
        return new TheoryData<DateTimeOffset, byte[]>
        {
            // negative offset
            {
                DateTimeOffset.UnixEpoch.ToOffset(TimeSpan.FromMinutes(-90)),
                "196912312230000000000-0130"u8.ToArray()
            },
            {
                DateTimeOffset.MaxValue.ToOffset(TimeSpan.FromMinutes(-90)),
                "999912312229599999999-0130"u8.ToArray()
            },
            // zero offset
            {
                DateTimeOffset.MinValue,
                "000101010000000000000+0000"u8.ToArray()
            },
            {
                DateTimeOffset.UnixEpoch,
                "197001010000000000000+0000"u8.ToArray()
            },
            {
                DateTimeOffset.MaxValue,
                "999912312359599999999+0000"u8.ToArray()
            },
            // positive offset
            {
                DateTimeOffset.MinValue.ToOffset(TimeSpan.FromMinutes(90)),
                "000101010130000000000+0130"u8.ToArray()
            },
            {
                DateTimeOffset.UnixEpoch.ToOffset(TimeSpan.FromMinutes(90)),
                "197001010130000000000+0130"u8.ToArray()
            }
        };
    }
}
